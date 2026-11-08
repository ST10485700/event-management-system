using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDay_Web.Filters;
using RaceDay_Web.Models;
using RaceDay_Web.Services;

namespace RaceDay_Web.Controllers;

[RoleAuthorize("Participant")]
public class EventsController : AppController
{
    public EventsController(RaceDayApiClient api) : base(api)
    {
    }

    // Participant home: upcoming events with filters.
    public async Task<IActionResult> Index(
        string? search,
        string? eventType,
        string? location,
        [FromQuery(Name = "from")] DateOnly? fromDate,
        [FromQuery(Name = "to")] DateOnly? toDate,
        bool includePast = false)
    {
        var model = new EventsIndexViewModel
        {
            Search = search,
            EventType = eventType,
            Location = location,
            From = fromDate,
            To = toDate,
            IncludePast = includePast
        };

        var query = new List<string>();

        if (!string.IsNullOrWhiteSpace(search))
            query.Add("search=" + Uri.EscapeDataString(search.Trim()));
        if (!string.IsNullOrWhiteSpace(eventType))
            query.Add("eventType=" + Uri.EscapeDataString(eventType.Trim()));
        if (!string.IsNullOrWhiteSpace(location))
            query.Add("location=" + Uri.EscapeDataString(location.Trim()));

        // Upcoming events only, unless the participant asks for past ones too.
        DateOnly? effectiveFrom = fromDate;
        if (effectiveFrom == null && !includePast)
        {
            effectiveFrom = DateOnly.FromDateTime(DateTime.Today);
        }

        if (effectiveFrom.HasValue)
            query.Add("from=" + effectiveFrom.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        if (toDate.HasValue)
            query.Add("to=" + toDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        var url = "api/Events" + (query.Count > 0 ? "?" + string.Join("&", query) : string.Empty);

        var result = await Api.GetAsync<List<EventSummary>>(url);

        if (result.IsUnauthorized) return ToLogin();
        if (result.IsForbidden) return ToAccessDenied();

        if (result.Success)
            model.Events = result.Data ?? new List<EventSummary>();
        else
            model.Error = result.Error;

        return View(model);
    }

    // Full event information, its categories and the Enter Event buttons.
    public async Task<IActionResult> Details(int id)
    {
        var eventResult = await Api.GetAsync<EventSummary>($"api/Events/{id}");

        if (eventResult.IsUnauthorized) return ToLogin();

        if (!eventResult.Success || eventResult.Data == null)
        {
            TempData["Error"] = eventResult.IsNotFound ? "We couldn't find that event." : eventResult.Error;
            return RedirectToAction(nameof(Index));
        }

        var model = new EventDetailsViewModel { Event = eventResult.Data };

        var categories = await Api.GetAsync<List<CategoryInfo>>($"api/Categories?eventId={id}");
        if (categories.IsUnauthorized) return ToLogin();

        if (categories.Success)
            model.Categories = categories.Data ?? new List<CategoryInfo>();
        else
            model.Error = categories.Error;

        var mine = await Api.GetAsync<List<EnrolmentInfo>>("api/Enrolments/my");
        if (mine.IsUnauthorized) return ToLogin();

        if (mine.Success && mine.Data != null)
        {
            model.MyEnrolmentStatus = mine.Data.ToDictionary(e => e.CategoryID, e => e.Status);
        }

        return View(model);
    }

    // Enter an event: records the enrolment straight away.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        var result = await Api.PostFormAsync<EnrolmentInfo>("api/Enrolments", new Dictionary<string, string>
        {
            ["categoryID"] = categoryId.ToString(CultureInfo.InvariantCulture)
        });

        if (result.IsUnauthorized) return ToLogin();

        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        TempData["Success"] = $"You're in! Your bib number is {result.Data?.BibNumber}.";
        return RedirectToAction("Index", "MyEnrolments");
    }
}
