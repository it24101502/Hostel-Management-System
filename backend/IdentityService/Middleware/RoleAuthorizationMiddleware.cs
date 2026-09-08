using IdentityService.Authorization;

namespace IdentityService.Middleware;

public class RoleAuthorizationMiddleware
{
    private readonly RequestDelegate _next;

    public RoleAuthorizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var roleRequirement = context
            .GetEndpoint()?
            .Metadata
            .GetMetadata<RequireRoleAttribute>();

        // The endpoint is public when it has no role requirement.
        if (roleRequirement is null)
        {
            await _next(context);
            return;
        }

        // The endpoint is protected, but the user is not authenticated.
        if (context.User.Identity?.IsAuthenticated != true)
        {
            context.Response.StatusCode =
                StatusCodes.Status401Unauthorized;

            await context.Response.WriteAsJsonAsync(new
            {
                message = "Authentication is required."
            });

            return;
        }

        bool hasAllowedRole = roleRequirement.AllowedRoles.Any(
            allowedRole => context.User.IsInRole(allowedRole));

        // The user is authenticated but does not have an allowed role.
        if (!hasAllowedRole)
        {
            context.Response.StatusCode =
                StatusCodes.Status403Forbidden;

            await context.Response.WriteAsJsonAsync(new
            {
                message = "Access denied."
            });

            return;
        }

        // The user has an allowed role, so continue to the endpoint.
        await _next(context);
    }
}