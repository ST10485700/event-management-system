using Microsoft.AspNetCore.Mvc;
using RaceDay_Web.Filters;
using RaceDay_Web.Models;
using RaceDay_Web.Services;

namespace RaceDay_Web.Controllers;

[RoleAuthorize("Participant")]
public class MyEnrolmentsController : AppController
{
    public MyEnrolmentsController(RaceDayApiClient api) : base(api)
    {
    }

    public async Task<IActionResult> Index()
    {
        var model = new EnrolmentsViewModel();

        var result = await Api.GetAsync<List<EnrolmentInfo>>("api/Enrolments/my");

        if (result.IsUnauthorized) return ToLogin();
        if (result.IsForbidden) return ToAccessDenied();

        if (result.Success)
            model.Enrolments = result.Data ?? new List<EnrolmentInfo>();
        else
            model.Error = result.Error;

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await Api.PutAsync<EnrolmentInfo>($"api/Enrolments/{id}/cancel");

        if (result.IsUnauthorized) return ToLogin();

        if (result.Success)
            TempData["Success"] = "Your enrolment has been cancelled.";
        else
            TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Index));
    }
}
