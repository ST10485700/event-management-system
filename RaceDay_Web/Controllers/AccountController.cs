using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay_Web.Models;
using RaceDay_Web.Services;

namespace RaceDay_Web.Controllers;

public class AccountController : Controller
{
    private readonly RaceDayApiClient _api;

    public AccountController(RaceDayApiClient api)
    {
        _api = api;
    }

    // ---------- Login ----------

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (HttpContext.Session.GetInt32(SessionKeys.UserId) != null)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _api.LoginAsync(model.Email.Trim(), model.Password);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }

        TempData["Success"] = $"Welcome back, {result.Data!.FullName}!";

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return LocalRedirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    // ---------- Register ----------

    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session.GetInt32(SessionKeys.UserId) != null)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var registered = await _api.RegisterAsync(model.FullName.Trim(), model.Email.Trim(), model.Password);

        if (!registered.Success)
        {
            ModelState.AddModelError(string.Empty, registered.Error);
            return View(model);
        }

        // Log the new participant straight in.
        var login = await _api.LoginAsync(model.Email.Trim(), model.Password);

        if (!login.Success)
        {
            TempData["Success"] = "Your account was created. Please log in.";
            return RedirectToAction(nameof(Login));
        }

        TempData["Success"] = $"Welcome to RaceDay, {login.Data!.FullName}!";
        return RedirectToAction("Index", "Home");
    }

    // ---------- Logout ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _api.LogoutAsync();

        TempData["Success"] = "You have been logged out.";
        return RedirectToAction(nameof(Login));
    }

    // ---------- Access denied ----------

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
