using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay_Web.Filters;
using RaceDay_Web.Models;
using RaceDay_Web.Services;

namespace RaceDay_Web.Controllers;

[RoleAuthorize]
public class ProfileController : AppController
{
    public ProfileController(RaceDayApiClient api) : base(api)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await Api.GetAsync<ProfileInfo>("api/Users/profile");

        if (result.IsUnauthorized) return ToLogin();

        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction("Index", "Home");
        }

        return View(new ProfileViewModel
        {
            FullName = result.Data.FullName,
            Email = result.Data.Email,
            ProfilePictureUrl = result.Data.ProfilePictureUrl,
            Role = result.Data.Role
        });
    }

    // Saves the name and email and, if one was chosen, the profile picture (sent to the API, which stores it in Azure).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        using var content = new MultipartFormDataContent
        {
            { new StringContent(model.FullName.Trim()), "fullName" },
            { new StringContent(model.Email.Trim()), "email" }
        };

        if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
        {
            var file = new StreamContent(model.ProfilePicture.OpenReadStream());
            if (MediaTypeHeaderValue.TryParse(model.ProfilePicture.ContentType, out var mediaType))
            {
                file.Headers.ContentType = mediaType;
            }

            content.Add(file, "profilePicture", model.ProfilePicture.FileName);
        }

        var result = await Api.SendMultipartAsync<ProfileInfo>(HttpMethod.Put, "api/Users/profile", content);

        if (result.IsUnauthorized) return ToLogin();

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View("Index", model);
        }

        if (result.Data != null)
        {
            // Keep the name in the menu bar up to date.
            HttpContext.Session.SetString(SessionKeys.FullName, result.Data.FullName);
        }

        TempData["Success"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Index));
    }
}
