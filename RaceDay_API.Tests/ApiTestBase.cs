using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace RaceDay_API.Tests;

public static class TestData
{
    // Seeded by DbSeeder
    public const string Password = "Password123!";
    public const string OrganiserEmail = "thabo@raceday.co.za";
    public const string OtherOrganiserEmail = "lerato@raceday.co.za";

    // Name used for the participants the tests register
    public const string ParticipantName = "Test Participant";
}

public static class HttpExtensions
{
    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(text);
        return doc.RootElement.Clone();
    }

    // Case-insensitive property lookup
    public static JsonElement Prop(this JsonElement element, string name)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }
        throw new KeyNotFoundException($"Property '{name}' was not found in: {element}");
    }
}

public abstract class ApiTestBase : IClassFixture<RaceDayFactory>
{
    protected readonly RaceDayFactory Factory;

    protected ApiTestBase(RaceDayFactory factory)
    {
        Factory = factory;
    }

    // Each client has its own cookie jar, so each one is its own session.
    protected HttpClient NewClient() =>
        Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

    protected static FormUrlEncodedContent Form(params (string Key, string Value)[] fields) =>
        new(fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value)));

    protected static string FutureDate(int daysAhead) =>
        DateOnly.FromDateTime(DateTime.Today.AddDays(daysAhead))
            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    protected async Task<HttpClient> LoginAsync(string email, string password = TestData.Password)
    {
        var client = NewClient();
        var response = await client.PostAsync("/api/Users/login",
            Form(("email", email), ("password", password)));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return client;
    }

    protected Task<HttpClient> LoginOrganiserAsync() => LoginAsync(TestData.OrganiserEmail);

    protected Task<HttpClient> LoginOtherOrganiserAsync() => LoginAsync(TestData.OtherOrganiserEmail);

    protected async Task<(HttpClient Client, string Email, int UserId)> RegisterParticipantAsync()
    {
        var email = $"participant_{Guid.NewGuid():N}@test.raceday.co.za";

        var register = await NewClient().PostAsync("/api/Users/register",
            Form(("fullName", TestData.ParticipantName),
                 ("email", email),
                 ("password", TestData.Password)));
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var userId = (await register.ReadJsonAsync()).Prop("userID").GetInt32();
        var client = await LoginAsync(email);
        return (client, email, userId);
    }

    protected async Task<int> CreateEventAsync(HttpClient organiser, int daysAhead = 30)
    {
        var response = await organiser.PostAsync("/api/Events", Form(
            ("name", $"Test Event {Guid.NewGuid():N}"),
            ("description", "Created by a unit test"),
            ("eventDate", FutureDate(daysAhead)),
            ("location", "Johannesburg"),
            ("eventType", "Running")));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.ReadJsonAsync()).Prop("eventID").GetInt32();
    }

    protected async Task<int> CreateCategoryAsync(HttpClient organiser, int eventId, int maxParticipants = 100)
    {
        var response = await organiser.PostAsync("/api/Categories", Form(
            ("eventID", eventId.ToString()),
            ("name", $"5km {Guid.NewGuid():N}"),
            ("distanceKm", "5"),
            ("maxParticipants", maxParticipants.ToString()),
            ("entryFee", "100")));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.ReadJsonAsync()).Prop("categoryID").GetInt32();
    }

    protected Task<HttpResponseMessage> EnrolAsync(HttpClient participant, int categoryId) =>
        participant.PostAsync("/api/Enrolments", Form(("categoryID", categoryId.ToString())));

    protected sealed record Scenario(
        HttpClient Organiser, HttpClient Participant, int ParticipantId,
        int EventId, int CategoryId, int EnrolmentId);

    // Organiser Thabo creates an event + category, and a new participant enrols in it.
    protected async Task<Scenario> CreateScenarioAsync(int maxParticipants = 100)
    {
        var organiser = await LoginOrganiserAsync();
        var eventId = await CreateEventAsync(organiser);
        var categoryId = await CreateCategoryAsync(organiser, eventId, maxParticipants);
        var (participant, _, userId) = await RegisterParticipantAsync();

        var response = await EnrolAsync(participant, categoryId);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var enrolmentId = (await response.ReadJsonAsync()).Prop("enrolmentID").GetInt32();

        return new Scenario(organiser, participant, userId, eventId, categoryId, enrolmentId);
    }
}