using System.Reflection;
using System.Security.Claims;
using LeaveService.Authorization;
using LeaveService.Controllers;
using LeaveService.Middleware;
using LeaveService.Models;
using Microsoft.AspNetCore.Http;

namespace LeaveService.Tests;

/// <summary>
/// HMS-6 role rules: wardens and hostel masters act on requests,
/// administrators may only view the report, students may do neither.
/// </summary>
public class LeaveStaffRoleAuthorizationTests
{
    [Theory]
    [InlineData(LeaveRoles.Warden)]
    [InlineData(LeaveRoles.HostelMaster)]
    [InlineData(LeaveRoles.Admin)]
    public async Task Report_IsOpenToWardenHostelMasterAndAdmin(string role)
    {
        var result = await InvokeAsync(
            typeof(StaffLeaveRequestsController),
            role);

        Assert.True(result.ReachedEndpoint);
    }

    [Fact]
    public async Task Report_IsClosedToStudents()
    {
        var result = await InvokeAsync(
            typeof(StaffLeaveRequestsController),
            LeaveRoles.Student);

        Assert.False(result.ReachedEndpoint);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    [Theory]
    [InlineData(LeaveRoles.Warden)]
    [InlineData(LeaveRoles.HostelMaster)]
    public async Task Actions_AreOpenToWardenAndHostelMaster(string role)
    {
        var result = await InvokeAsync(
            typeof(StaffLeaveActionsController),
            role);

        Assert.True(result.ReachedEndpoint);
    }

    [Theory]
    [InlineData(LeaveRoles.Student)]
    [InlineData(LeaveRoles.Admin)]
    public async Task Actions_AreClosedToStudentsAndAdministrators(string role)
    {
        var result = await InvokeAsync(
            typeof(StaffLeaveActionsController),
            role);

        Assert.False(result.ReachedEndpoint);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    [Theory]
    [InlineData(typeof(StaffLeaveRequestsController))]
    [InlineData(typeof(StaffLeaveActionsController))]
    public async Task StaffEndpoints_RejectUnauthenticatedRequests(
        Type controllerType)
    {
        var result = await InvokeAsync(controllerType, role: null);

        Assert.False(result.ReachedEndpoint);
        Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
    }

    [Fact]
    public void ReportController_DeclaresTheExpectedRoles()
    {
        Assert.Equal(
            new[] { LeaveRoles.Warden, LeaveRoles.HostelMaster, LeaveRoles.Admin },
            GetAllowedRoles(typeof(StaffLeaveRequestsController)));
    }

    [Fact]
    public void ActionsController_DeclaresTheExpectedRoles()
    {
        Assert.Equal(
            new[] { LeaveRoles.Warden, LeaveRoles.HostelMaster },
            GetAllowedRoles(typeof(StaffLeaveActionsController)));
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
                "leave-staff-test-endpoint"));

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
