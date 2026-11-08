using System.Globalization;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay_Web.Filters;
using RaceDay_Web.Models;
using RaceDay_Web.Services;

namespace RaceDay_Web.Controllers;

[RoleAuthorize("Organiser")]
public class OrganiserController : AppController
{
    public OrganiserController(RaceDayApiClient api) : base(api)
    {
    }

    private int CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId) ?? 0;

    private static string Inv(decimal value) => value.ToString(CultureInfo.InvariantCulture);
    private static string Inv(int value) => value.ToString(CultureInfo.InvariantCulture);

    // =====================================================================
    // Dashboard
    // =====================================================================

    public async Task<IActionResult> Dashboard()
    {
        var model = new DashboardViewModel();
        var result = await Api.GetAsync<List<MyEventInfo>>("api/Events/mine");

        if (result.IsUnauthorized) return ToLogin();
        if (result.IsForbidden) return ToAccessDenied();

        if (result.Success)
            model.Events = result.Data ?? new List<MyEventInfo>();
        else
            model.Error = result.Error;

        return View(model);
    }

    // =====================================================================
    // Events: list, create, edit, delete
    // =====================================================================

    public async Task<IActionResult> Events()
    {
        var model = new OrganiserEventsViewModel();
        var result = await Api.GetAsync<List<MyEventInfo>>("api/Events/mine");

        if (result.IsUnauthorized) return ToLogin();
        if (result.IsForbidden) return ToAccessDenied();

        if (result.Success)
            model.Events = result.Data ?? new List<MyEventInfo>();
        else
            model.Error = result.Error;

        return View(model);
    }

    [HttpGet]
    public IActionResult CreateEvent()
    {
        return View("EventForm", new EventFormModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(EventFormModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("EventForm", model);
        }

        using var content = BuildEventContent(model);
        var result = await Api.SendMultipartAsync<EventSummary>(HttpMethod.Post, "api/Events", content);

        if (result.IsUnauthorized) return ToLogin();
        if (result.IsForbidden) return ToAccessDenied();

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View("EventForm", model);
        }

        TempData["Success"] = $"\"{model.Name.Trim()}\" has been created. Add some categories so people can enter.";
        return RedirectToAction(nameof(Events));
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var result = await Api.GetAsync<EventSummary>($"api/Events/{id}");

        if (result.IsUnauthorized) return ToLogin();

        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.IsNotFound ? "We couldn't find that event." : result.Error;
            return RedirectToAction(nameof(Events));
        }

        if (result.Data.OrganiserID != CurrentUserId)
        {
            TempData["Error"] = "You can only edit your own events.";
            return RedirectToAction(nameof(Events));
        }

        return View("EventForm", new EventFormModel
        {
            EventID = result.Data.EventID,
            Name = result.Data.Name,
            Description = result.Data.Description,
            EventDate = result.Data.EventDate,
            Location = result.Data.Location,
            EventType = result.Data.EventType,
            DistanceKm = result.Data.DistanceKm,
            ExistingBannerUrl = result.Data.BannerUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(EventFormModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("EventForm", model);
        }

        using var content = BuildEventContent(model);
        var result = await Api.SendMultipartAsync<EventSummary>(HttpMethod.Put, $"api/Events/{model.EventID}", content);

        if (result.IsUnauthorized) return ToLogin();
        if (result.IsForbidden) return ToAccessDenied();

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View("EventForm", model);
        }

        TempData["Success"] = $"\"{model.Name.Trim()}\" has been updated.";
        return RedirectToAction(nameof(Events));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var result = await Api.DeleteAsync($"api/Events/{id}");

        if (result.IsUnauthorized) return ToLogin();

        if (result.Success)
            TempData["Success"] = "The event has been deleted.";
        else
            TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Events));
    }

    // Builds the multipart form sent to the API (the optional banner goes to Azure through the API).
    private static MultipartFormDataContent BuildEventContent(EventFormModel model)
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent(model.Name.Trim()), "name" },
            { new StringContent(model.EventDate!.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)), "eventDate" },
            { new StringContent(model.Location.Trim()), "location" },
            { new StringContent(model.EventType), "eventType" },
            { new StringContent(Inv(model.DistanceKm ?? 0m)), "distanceKm" }
        };

        if (!string.IsNullOrWhiteSpace(model.Description))
        {
            content.Add(new StringContent(model.Description.Trim()), "description");
        }

        if (model.Banner != null && model.Banner.Length > 0)
        {
            var file = new StreamContent(model.Banner.OpenReadStream());
            if (MediaTypeHeaderValue.TryParse(model.Banner.ContentType, out var mediaType))
            {
                file.Headers.ContentType = mediaType;
            }

            content.Add(file, "banner", model.Banner.FileName);
        }

        return content;
    }

    // =====================================================================
    // Categories for one event
    // =====================================================================

    [HttpGet]
    public async Task<IActionResult> Categories(int eventId)
    {
        var (model, redirect) = await LoadCategoriesAsync(eventId, new CategoryFormModel { EventID = eventId });
        if (redirect != null) return redirect;

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(CategoryFormModel form)
    {
        if (!ModelState.IsValid)
        {
            var (model, redirect) = await LoadCategoriesAsync(form.EventID, form);
            if (redirect != null) return redirect;

            return View("Categories", model);
        }

        var result = await Api.PostFormAsync<CategoryInfo>("api/Categories", new Dictionary<string, string>
        {
            ["eventID"] = Inv(form.EventID),
            ["name"] = form.Name.Trim(),
            ["distanceKm"] = Inv(form.DistanceKm ?? 0m),
            ["maxParticipants"] = Inv(form.MaxParticipants ?? 0),
            ["entryFee"] = Inv(form.EntryFee ?? 0m)
        });

        if (result.IsUnauthorized) return ToLogin();

        if (result.Success)
        {
            TempData["Success"] = $"Category \"{form.Name.Trim()}\" added.";
            return RedirectToAction(nameof(Categories), new { eventId = form.EventID });
        }

        ModelState.AddModelError(string.Empty, result.Error);
        var (failedModel, failedRedirect) = await LoadCategoriesAsync(form.EventID, form);
        if (failedRedirect != null) return failedRedirect;

        return View("Categories", failedModel);
    }

    [HttpGet]
    public async Task<IActionResult> EditCategory(int id)
    {
        var result = await Api.GetAsync<CategoryInfo>($"api/Categories/{id}");

        if (result.IsUnauthorized) return ToLogin();

        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.IsNotFound ? "We couldn't find that category." : result.Error;
            return RedirectToAction(nameof(Events));
        }

        return View(new CategoryFormModel
        {
            CategoryID = result.Data.CategoryID,
            EventID = result.Data.EventID,
            EventName = result.Data.EventName,
            Name = result.Data.Name,
            DistanceKm = result.Data.DistanceKm,
            MaxParticipants = result.Data.MaxParticipants,
            EntryFee = result.Data.EntryFee
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(CategoryFormModel form)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var result = await Api.PutFormAsync<CategoryInfo>($"api/Categories/{form.CategoryID}", new Dictionary<string, string>
        {
            ["name"] = form.Name.Trim(),
            ["distanceKm"] = Inv(form.DistanceKm ?? 0m),
            ["maxParticipants"] = Inv(form.MaxParticipants ?? 0),
            ["entryFee"] = Inv(form.EntryFee ?? 0m)
        });

        if (result.IsUnauthorized) return ToLogin();

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(form);
        }

        TempData["Success"] = "The category has been updated.";
        return RedirectToAction(nameof(Categories), new { eventId = form.EventID });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id, int eventId)
    {
        var result = await Api.DeleteAsync($"api/Categories/{id}");

        if (result.IsUnauthorized) return ToLogin();

        if (result.Success)
            TempData["Success"] = "The category has been deleted.";
        else
            TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Categories), new { eventId });
    }

    private async Task<(CategoriesViewModel? Model, IActionResult? Redirect)> LoadCategoriesAsync(int eventId, CategoryFormModel form)
    {
        var ev = await Api.GetAsync<EventSummary>($"api/Events/{eventId}");

        if (ev.IsUnauthorized) return (null, ToLogin());

        if (!ev.Success || ev.Data == null)
        {
            TempData["Error"] = ev.IsNotFound ? "We couldn't find that event." : ev.Error;
            return (null, RedirectToAction(nameof(Events)));
        }

        if (ev.Data.OrganiserID != CurrentUserId)
        {
            TempData["Error"] = "You can only manage categories for your own events.";
            return (null, RedirectToAction(nameof(Events)));
        }

        var model = new CategoriesViewModel { Event = ev.Data, NewCategory = form };

        var categories = await Api.GetAsync<List<CategoryInfo>>($"api/Categories?eventId={eventId}");
        if (categories.IsUnauthorized) return (null, ToLogin());

        if (categories.Success)
            model.Categories = categories.Data ?? new List<CategoryInfo>();
        else
            model.Error = categories.Error;

        return (model, null);
    }

    // =====================================================================
    // Enrolments for one event
    // =====================================================================

    public async Task<IActionResult> Enrolments(int? eventId)
    {
        var model = new EventEnrolmentsViewModel();

        var events = await Api.GetAsync<List<MyEventInfo>>("api/Events/mine");
        if (events.IsUnauthorized) return ToLogin();
        if (events.IsForbidden) return ToAccessDenied();

        if (!events.Success)
        {
            model.Error = events.Error;
            return View(model);
        }

        model.Events = events.Data ?? new List<MyEventInfo>();
        model.SelectedEventId = eventId ?? model.Events.FirstOrDefault()?.EventID;

        if (model.SelectedEventId.HasValue)
        {
            var enrolments = await Api.GetAsync<List<EnrolmentInfo>>($"api/Enrolments?eventId={model.SelectedEventId.Value}");
            if (enrolments.IsUnauthorized) return ToLogin();

            if (enrolments.Success)
                model.Enrolments = enrolments.Data ?? new List<EnrolmentInfo>();
            else
                model.Error = enrolments.Error;
        }

        return View(model);
    }

    // =====================================================================
    // Results entry for one event
    // =====================================================================

    public async Task<IActionResult> Results(int? eventId)
    {
        var model = new ResultsEntryViewModel();

        var events = await Api.GetAsync<List<MyEventInfo>>("api/Events/mine");
        if (events.IsUnauthorized) return ToLogin();
        if (events.IsForbidden) return ToAccessDenied();

        if (!events.Success)
        {
            model.Error = events.Error;
            return View(model);
        }

        model.Events = events.Data ?? new List<MyEventInfo>();
        model.SelectedEventId = eventId ?? model.Events.FirstOrDefault()?.EventID;

        if (model.SelectedEventId.HasValue)
        {
            var id = model.SelectedEventId.Value;

            var enrolments = await Api.GetAsync<List<EnrolmentInfo>>($"api/Enrolments?eventId={id}");
            if (enrolments.IsUnauthorized) return ToLogin();

            var results = await Api.GetAsync<List<ResultInfo>>($"api/Results?eventId={id}");
            if (results.IsUnauthorized) return ToLogin();

            if (!enrolments.Success)
            {
                model.Error = enrolments.Error;
            }
            else if (!results.Success)
            {
                model.Error = results.Error;
            }
            else
            {
                var resultsByEnrolment = (results.Data ?? new List<ResultInfo>())
                    .ToDictionary(r => r.EnrolmentID, r => r);

                model.Rows = (enrolments.Data ?? new List<EnrolmentInfo>())
                    .Where(e => e.Status != "Cancelled")
                    .OrderBy(e => e.CategoryName)
                    .ThenBy(e => e.BibNumber)
                    .Select(e => new ResultRow
                    {
                        Enrolment = e,
                        Result = resultsByEnrolment.TryGetValue(e.EnrolmentID, out var r) ? r : null
                    })
                    .ToList();
            }
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CaptureResult(int eventId, int enrolmentId, string? finishTime, int? position)
    {
        if (!TryReadResult(finishTime, position, out var time, out var place))
        {
            TempData["Error"] = "Enter the finish time as hours:minutes:seconds (for example 00:45:30) and a position of 1 or more.";
            return RedirectToAction(nameof(Results), new { eventId });
        }

        var result = await Api.PostFormAsync<ResultInfo>("api/Results", new Dictionary<string, string>
        {
            ["enrolmentID"] = Inv(enrolmentId),
            ["finishTime"] = time.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture),
            ["position"] = Inv(place)
        });

        if (result.IsUnauthorized) return ToLogin();

        if (result.Success)
            TempData["Success"] = "Result captured.";
        else
            TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Results), new { eventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateResult(int eventId, int resultId, string? finishTime, int? position)
    {
        if (!TryReadResult(finishTime, position, out var time, out var place))
        {
            TempData["Error"] = "Enter the finish time as hours:minutes:seconds (for example 00:45:30) and a position of 1 or more.";
            return RedirectToAction(nameof(Results), new { eventId });
        }

        var result = await Api.PutFormAsync<ResultInfo>($"api/Results/{resultId}", new Dictionary<string, string>
        {
            ["finishTime"] = time.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture),
            ["position"] = Inv(place)
        });

        if (result.IsUnauthorized) return ToLogin();

        if (result.Success)
            TempData["Success"] = "Result updated.";
        else
            TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Results), new { eventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteResult(int eventId, int resultId)
    {
        var result = await Api.DeleteAsync($"api/Results/{resultId}");

        if (result.IsUnauthorized) return ToLogin();

        if (result.Success)
            TempData["Success"] = "Result removed.";
        else
            TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Results), new { eventId });
    }

    // Accepts h:mm:ss or hh:mm:ss (never mm:ss, to avoid minutes being read as hours).
    private static bool TryReadResult(string? finishTime, int? position, out TimeSpan time, out int place)
    {
        time = TimeSpan.Zero;
        place = 0;

        if (string.IsNullOrWhiteSpace(finishTime) || position is null or < 1)
        {
            return false;
        }

        if (!TimeSpan.TryParseExact(finishTime.Trim(), @"h\:mm\:ss", CultureInfo.InvariantCulture, out time)
            || time <= TimeSpan.Zero)
        {
            return false;
        }

        place = position.Value;
        return true;
    }
}
