using Microsoft.AspNetCore.Mvc;
using RaceDay_Web.Services;

namespace RaceDay_Web.Controllers;

/// <summary>Base class for pages that call the API.</summary>
public abstract class AppController : Controller
{
    protected readonly RaceDayApiClient Api;

    protected AppController(RaceDayApiClient api)
    {
        Api = api;
    }

    // The API session ended (for example it expired): send the user back to log in.
    protected IActionResult ToLogin()
    {
        TempData["Error"] = "Your session has ended. Please log in again.";
        return RedirectToAction("Login", "Account");
    }

    protected IActionResult ToAccessDenied() => RedirectToAction("AccessDenied", "Account");
}
