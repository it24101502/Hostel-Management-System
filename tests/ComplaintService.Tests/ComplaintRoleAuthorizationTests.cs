using System.Reflection;
using System.Security.Claims;
using ComplaintService.Authorization;
using ComplaintService.Controllers;
using ComplaintService.Middleware;
using ComplaintService.Models;
using Microsoft.AspNetCore.Http;

namespace ComplaintService.Tests;

public class ComplaintRoleAuthorizationTests
{
    [Fact]
    public async Task StudentEndpoints_AllowStudent()
    {
        var result = await InvokeAsync(typeof(StudentComplaintsController), ComplaintRoles.Student);
        Assert.True(result.ReachedEndpoint);
    }

    [Theory]
    [InlineData(ComplaintRoles.Warden)]
    [InlineData(ComplaintRoles.HostelMaster)]
    [InlineData(ComplaintRoles.Admin)]
    public async Task StudentEndpoints_RejectStaffRoles(string role)
    {
        var result = await InvokeAsync(typeof(StudentComplaintsController), role);
        Assert.False(result.ReachedEndpoint);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    [Theory]
    [InlineData(ComplaintRoles.Warden)]
    [InlineData(ComplaintRoles.HostelMaster)]
    [InlineData(ComplaintRoles.Admin)]
    public async Task StaffEndpoints_AllowStaffRoles(string role)
    {
        var result = await InvokeAsync(typeof(StaffComplaintsController), role);
        Assert.True(result.ReachedEndpoint);
    }

    [Fact]
    public async Task StaffEndpoints_RejectStudents()
    {
        var result = await InvokeAsync(typeof(StaffComplaintsController), ComplaintRoles.Student);
        Assert.False(result.ReachedEndpoint);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    [Theory]
    [InlineData(typeof(StudentComplaintsController))]
    [InlineData(typeof(StaffComplaintsController))]
    public async Task ComplaintEndpoints_RejectUnauthenticatedRequests(Type controllerType)
    {
        var result = await InvokeAsync(controllerType, role: null);
        Assert.False(result.ReachedEndpoint);
        Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
    }

    private sealed record MiddlewareResult(bool ReachedEndpoint, int StatusCode);

    private static async Task<MiddlewareResult> InvokeAsync(Type controllerType, string? role)
    {
        var requirement = controllerType.GetCustomAttribute<RequireRoleAttribute>();
        Assert.NotNull(requirement);

        bool reached = false;
        var middleware = new RoleAuthorizationMiddleware(_ => { reached = true; return Task.CompletedTask; });

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(requirement),
            "complaint-test-endpoint"));

        if (role is not null)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Role, role) }, "Test"));
        }

        await middleware.InvokeAsync(context);
        return new MiddlewareResult(reached, context.Response.StatusCode);
    }
}