using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay_API.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _roles;

    public SessionAuthorizeAttribute(params string[] roles)
    {
        _roles = roles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetInt32("UserID");
        var role = session.GetString("Role");

        if (userId == null)
        {
            context.Result = new UnauthorizedObjectResult(
                new { message = "You must be logged in to access this resource." });
            return;
        }

        if (_roles.Length > 0 && (role == null || !_roles.Contains(role)))
        {
            context.Result = new ObjectResult(
                new { message = "Your role does not have permission to access this resource." })
            { StatusCode = StatusCodes.Status403Forbidden };
        }
    }
}