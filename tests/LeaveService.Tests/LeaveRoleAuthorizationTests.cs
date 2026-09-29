using System.Reflection;
using System.Security.Claims;
using LeaveService.Authorization;
using LeaveService.Controllers;
using LeaveService.Middleware;
using LeaveService.Models;
using Microsoft.AspNetCore.Http;

namespace LeaveService.Tests;

/// <summary>
/// Verifies that RoleAuthorizationMiddleware enforces the roles
/// declared on the leave controllers, the same way it does for
/// the other services.
/// </summary>
public class LeaveRoleAuthorizationTests
{
    [Fact]
    public async Task StudentEndpoints_AllowStudent()
    {
        var result = await InvokeAsync(
            typeof(StudentLeaveRequestsController),
            LeaveRoles.Student);

        Assert.True(result.ReachedEndpoint);
    }

    [Theory]
    [InlineData(LeaveRoles.Warden)]
    [InlineData(LeaveRoles.HostelMaster)]
    [InlineData(LeaveRoles.Admin)]
    public async Task StudentEndpoints_RejectOtherRoles(string role)
    {
        var result = await InvokeAsync(
            typeof(StudentLeaveRequestsController),
            role);

        Assert.False(result.ReachedEndpoint);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    [Theory]
    [InlineData(LeaveRoles.Warden)]
    [InlineData(LeaveRoles.HostelMaster)]
    public async Task StaffNotificationEndpoints_AllowWardenAndHostelMaster(
        string role)
    {
        var result = await InvokeAsync(
            typeof(StaffLeaveNotificationsController),
            role);

        Assert.True(result.ReachedEndpoint);
    }

    [Theory]
    [InlineData(LeaveRoles.Student)]
    [InlineData(LeaveRoles.Admin)]
    public async Task StaffNotificationEndpoints_RejectOtherRoles(
        string role)
    {
        var result = await InvokeAsync(
            typeof(StaffLeaveNotificationsController),
            role);

        Assert.False(result.ReachedEndpoint);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    [Theory]
    [InlineData(typeof(StudentLeaveRequestsController))]
    [InlineData(typeof(StaffLeaveNotificationsController))]
    public async Task LeaveEndpoints_RejectUnauthenticatedRequests(
        Type controllerType)
    {
        var result = await InvokeAsync(controllerType, role: null);

        Assert.False(result.ReachedEndpoint);
        Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
    }

    [Fact]
    public void StudentController_RequiresOnlyTheStudentRole()
    {
        var roles = GetAllowedRoles(typeof(StudentLeaveRequestsController));

        Assert.Equal(new[] { LeaveRoles.Student }, roles);
    }

    [Fact]
    public void StaffNotificationController_RequiresWardenOrHostelMaster()
    {
        var roles = GetAllowedRoles(typeof(StaffLeaveNotificationsController));

        Assert.Equal(
            new[] { LeaveRoles.Warden, LeaveRoles.HostelMaster },
            roles);
    }

    private static string[] GetAllowedRoles(Type controllerType)
    {
        var attribute =
            controllerType.GetCustomAttribute<RequireRoleAttribute>();

        Assert.NotNull(attribute);

        return attribute.AllowedRoles.ToArray();
    }

    private sealed record MiddlewareResult(
        bool ReachedEndpoint,
        int StatusCode);

    private static async Task<MiddlewareResult> InvokeAsync(
        Type controllerType,
        string? role)
    {
        var requirement =
            controllerType.GetCustomAttribute<RequireRoleAttribute>();

        Assert.NotNull(requirement);

        bool reachedEndpoint = false;

        var middleware = new RoleAuthorizationMiddleware(
            _ =>
            {
                reachedEndpoint = true;
                return Task.CompletedTask;
            });

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        context.SetEndpoint(
            new Endpoint(
                _ => Task.CompletedTask,
                new EndpointMetadataCollection(requirement),
                "leave-test-endpoint"));

        if (role is not null)
        {
            context.User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.Role, role) },
                    authenticationType: "Test"));
        }

        await middleware.InvokeAsync(context);

        return new MiddlewareResult(
            reachedEndpoint,
            context.Response.StatusCode);
    }
}
