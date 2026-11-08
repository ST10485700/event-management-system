using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using RaceDay_Web.Models;

namespace RaceDay_Web.Services;

/// <summary>
/// The only class in the MVC project that talks to the RaceDay API.
/// It attaches the logged-in user's API session cookie to every call and turns every
/// failure into a friendly ApiResult, so the MVC app has no database access and no business rules.
/// </summary>
public class RaceDayApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;

    public RaceDayApiClient(HttpClient http, IHttpContextAccessor accessor)
    {
        _http = http;
        _accessor = accessor;
    }

    private ISession Session => _accessor.HttpContext!.Session;

    // ---------- Authentication ----------

    public Task<ApiResult<RegisterResponse>> RegisterAsync(string fullName, string email, string password)
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["fullName"] = fullName,
            ["email"] = email,
            ["password"] = password
        });

        return ExecuteAsync<RegisterResponse>(HttpMethod.Post, "api/Users/register", form, attachCookie: false);
    }

    /// <summary>Logs in through the API and, on success, stores the user and the API session cookie in the MVC session.</summary>
    public async Task<ApiResult<LoginResponse>> LoginAsync(string email, string password)
    {
        string? apiCookie = null;

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = email,
            ["password"] = password
        });

        var result = await ExecuteAsync<LoginResponse>(HttpMethod.Post, "api/Users/login", form,
            inspect: response =>
            {
                if (response.Headers.TryGetValues("Set-Cookie", out var values))
                {
                    apiCookie = values
                        .Select(v => v.Split(';')[0])
                        .FirstOrDefault(v => v.StartsWith(".AspNetCore.Session=", StringComparison.OrdinalIgnoreCase));
                }
            },
            attachCookie: false);

        if (!result.Success || result.Data == null)
        {
            return result;
        }

        if (apiCookie == null)
        {
            return new ApiResult<LoginResponse>
            {
                Success = false,
                StatusCode = 500,
                Error = "We could not start your session. Please try again."
            };
        }

        Session.Clear();
        Session.SetInt32(SessionKeys.UserId, result.Data.UserID);
        Session.SetString(SessionKeys.FullName, result.Data.FullName);
        Session.SetString(SessionKeys.Role, result.Data.Role);
        Session.SetString(SessionKeys.ApiCookie, apiCookie);

        return result;
    }

    /// <summary>Ends the API session (best effort) and clears the MVC session.</summary>
    public async Task LogoutAsync()
    {
        await ExecuteAsync<object>(HttpMethod.Post, "api/Users/logout");
        Session.Clear();
    }

    // ---------- Generic helpers used by the pages ----------

    public Task<ApiResult<T>> GetAsync<T>(string url) =>
        ExecuteAsync<T>(HttpMethod.Get, url);

    public Task<ApiResult<T>> PostAsync<T>(string url) =>
        ExecuteAsync<T>(HttpMethod.Post, url);

    public Task<ApiResult<T>> PutAsync<T>(string url) =>
        ExecuteAsync<T>(HttpMethod.Put, url);

    public Task<ApiResult<T>> PostFormAsync<T>(string url, Dictionary<string, string> fields) =>
        ExecuteAsync<T>(HttpMethod.Post, url, new FormUrlEncodedContent(fields));

    public Task<ApiResult<T>> PutFormAsync<T>(string url, Dictionary<string, string> fields) =>
        ExecuteAsync<T>(HttpMethod.Put, url, new FormUrlEncodedContent(fields));

    public Task<ApiResult<T>> SendMultipartAsync<T>(HttpMethod method, string url, MultipartFormDataContent content) =>
        ExecuteAsync<T>(method, url, content);

    public Task<ApiResult<object>> DeleteAsync(string url) =>
        ExecuteAsync<object>(HttpMethod.Delete, url);

    // ---------- Core ----------

    private async Task<ApiResult<T>> ExecuteAsync<T>(
        HttpMethod method,
        string url,
        HttpContent? content = null,
        Action<HttpResponseMessage>? inspect = null,
        bool attachCookie = true)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url) { Content = content };

            if (attachCookie)
            {
                var cookie = Session.GetString(SessionKeys.ApiCookie);
                if (!string.IsNullOrEmpty(cookie))
                {
                    request.Headers.TryAddWithoutValidation("Cookie", cookie);
                }
            }

            using var response = await _http.SendAsync(request);
            inspect?.Invoke(response);
            var status = (int)response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                T? data = default;
                if (response.StatusCode != HttpStatusCode.NoContent)
                {
                    data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
                }

                return new ApiResult<T> { Success = true, StatusCode = status, Data = data };
            }

            // The API session expired or was lost: log the user out of the MVC app too.
            if (status == 401 && attachCookie && Session.GetInt32(SessionKeys.UserId) != null)
            {
                Session.Clear();
            }

            return new ApiResult<T>
            {
                Success = false,
                StatusCode = status,
                Error = await ReadErrorAsync(response)
            };
        }
        catch (HttpRequestException)
        {
            return Failure<T>(503, "RaceDay could not reach the server. Please try again in a moment.");
        }
        catch (TaskCanceledException)
        {
            return Failure<T>(503, "The server took too long to respond. Please try again.");
        }
        catch (JsonException)
        {
            return Failure<T>(500, "The server sent a response we could not understand.");
        }
    }

    private static ApiResult<T> Failure<T>(int status, string message) =>
        new() { Success = false, StatusCode = status, Error = message };

    // Turns any API error body into a short, friendly sentence.
    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();

        if (!string.IsNullOrWhiteSpace(text))
        {
            try
            {
                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.String)
                {
                    return root.GetString() ?? string.Empty;
                }

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                    {
                        return message.GetString() ?? string.Empty;
                    }

                    if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                    {
                        var messages = new List<string>();
                        foreach (var field in errors.EnumerateObject())
                        {
                            if (field.Value.ValueKind != JsonValueKind.Array) continue;
                            foreach (var item in field.Value.EnumerateArray())
                            {
                                var line = item.GetString();
                                if (!string.IsNullOrWhiteSpace(line)) messages.Add(line);
                            }
                        }

                        if (messages.Count > 0) return string.Join(" ", messages);
                    }
                }
            }
            catch (JsonException)
            {
                // Not JSON: fall through to the generic messages below.
            }
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Please log in to continue.",
            HttpStatusCode.Forbidden => "You do not have permission to do that.",
            HttpStatusCode.NotFound => "We could not find what you were looking for.",
            HttpStatusCode.Conflict => "That conflicts with existing information.",
            HttpStatusCode.BadRequest => "Some of the information was not valid. Please check and try again.",
            _ => "Something went wrong. Please try again."
        };
    }
}
