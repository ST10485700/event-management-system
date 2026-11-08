using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay_Web.Services;

namespace RaceDay_Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        var role = HttpContext.Session.GetString(SessionKeys.Role);

        // Each role lands on its own home page.
        if (role == "Participant")
        {
            return RedirectToAction("Index", "Events");
        }

        if (role == "Organiser")
        {
            return RedirectToAction("Dashboard", "Organiser");
        }

        return View();
    }

    // Friendly error page for exceptions and for 404, 403 and so on. Never shows raw error text.
    public IActionResult Error(int? code)
    {
        var status = code ?? 500;

        ViewBag.Code = status;
        ViewBag.Title = status == 404 ? "Page not found" : "Something went wrong";
        ViewBag.Message = status switch
        {
            404 => "We couldn't find the page you were looking for. It may have moved or no longer exists.",
            403 => "You don't have permission to view that page.",
            _ => "Something went wrong on our side. Please try again in a moment."
        };

        return View();
    }
}
