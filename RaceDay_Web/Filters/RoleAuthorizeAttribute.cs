using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay_Web.Services;

namespace RaceDay_Web.Filters;

/// <summary>
/// Protects MVC pages using the MVC session.
/// Not logged in: redirected to the login page. Wrong role: redirected to the access-denied page.
/// Use [RoleAuthorize] for any logged-in user, or [RoleAuthorize("Organiser")] for one role.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RoleAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _roles;

    public RoleAuthorizeAttribute(params string[] roles)
    {
        _roles = roles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;

        if (session.GetInt32(SessionKeys.UserId) == null)
        {
            var request = context.HttpContext.Request;
            var returnUrl = request.Method == HttpMethods.Get ? request.Path + request.QueryString : null;

            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        var role = session.GetString(SessionKeys.Role);
        if (_roles.Length > 0 && (role == null || !_roles.Contains(role)))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}
