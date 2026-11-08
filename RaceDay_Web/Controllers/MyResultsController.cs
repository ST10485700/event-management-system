using Microsoft.AspNetCore.Mvc;
using RaceDay_Web.Filters;
using RaceDay_Web.Models;
using RaceDay_Web.Services;

namespace RaceDay_Web.Controllers;

[RoleAuthorize("Participant")]
public class MyResultsController : AppController
{
    public MyResultsController(RaceDayApiClient api) : base(api)
    {
    }

    public async Task<IActionResult> Index()
    {
        var model = new ResultsViewModel();

        var result = await Api.GetAsync<List<ResultInfo>>("api/Results/my");

        if (result.IsUnauthorized) return ToLogin();
        if (result.IsForbidden) return ToAccessDenied();

        if (result.Success)
            model.Results = result.Data ?? new List<ResultInfo>();
        else
            model.Error = result.Error;

        return View(model);
    }
}
