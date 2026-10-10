using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using NoticeService.Authorization;
using NoticeService.Middleware;
using NoticeService.Models;
using NoticeService.Repositories;
using NoticeService.Services;
using Xunit;

namespace NoticeService.Tests;

/// <summary>
/// Small building blocks of NoticeService: the DateOnly type handler,
/// the system clock, the shared constants (which must match the database
/// CHECK constraints) and the role middleware.
/// </summary>
public class NoticeInfrastructureTests
{
    // ---------- DateOnlyTypeHandler ----------

    [Fact]
    public void TypeHandler_SetValue_SendsADateParameter()
    {
        var parameter = new Mock<IDbDataParameter>();
        parameter.SetupAllProperties();

        new DateOnlyTypeHandler().SetValue(
            parameter.Object,
            new DateOnly(2026, 10, 10));

        Assert.Equal(DbType.Date, parameter.Object.DbType);
        Assert.Equal(new DateTime(2026, 10, 10), parameter.Object.Value);
    }

    [Fact]
    public void TypeHandler_Parse_AcceptsADateTimeFromMySql()
    {
        DateOnly result = new DateOnlyTypeHandler()
            .Parse(new DateTime(2026, 10, 10, 13, 45, 0));

        Assert.Equal(new DateOnly(2026, 10, 10), result);
    }

    [Fact]
    public void TypeHandler_Parse_AcceptsADateOnlyUnchanged()
    {
        var date = new DateOnly(2027, 1, 31);

        Assert.Equal(date, new DateOnlyTypeHandler().Parse(date));
    }

    [Fact]
    public void TypeHandler_Parse_AcceptsAnIsoDateString()
    {
        Assert.Equal(
            new DateOnly(2026, 12, 25),
            new DateOnlyTypeHandler().Parse("2026-12-25"));
    }

    [Fact]
    public void TypeHandler_RoundTrip_KeepsTheSameDate()
    {
        var handler = new DateOnlyTypeHandler();
        var parameter = new Mock<IDbDataParameter>();
        parameter.SetupAllProperties();

        var original = new DateOnly(2026, 2, 28);

        handler.SetValue(parameter.Object, original);

        Assert.Equal(original, handler.Parse(parameter.Object.Value!));
    }

    // ---------- SystemDateTimeProvider ----------

    [Fact]
    public void SystemClock_TodayIsTheCurrentUtcDate()
    {
        DateOnly before = DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly today = new SystemDateTimeProvider().Today;
        DateOnly after = DateOnly.FromDateTime(DateTime.UtcNow);

        // Tolerates the test running across midnight UTC.
        Assert.InRange(today.DayNumber, before.DayNumber, after.DayNumber);
    }

    [Fact]
    public void SystemClock_UtcNowIsInUtc()
    {
        DateTime now = new SystemDateTimeProvider().UtcNow;

        Assert.Equal(DateTimeKind.Utc, now.Kind);
        Assert.InRange(
            now,
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow.AddMinutes(1));
    }

    // ---------- constants that mirror the database ----------

    [Fact]
    public void NoticeTypes_MatchTheDatabaseConstraint()
    {
        Assert.Equal("NOTICE", NoticeTypes.Notice);
        Assert.Equal("SCHEDULE", NoticeTypes.Schedule);
        Assert.Equal(2, NoticeTypes.All.Count);
    }

    [Theory]
    [InlineData("NOTICE")]
    [InlineData("notice")]
    [InlineData("Schedule")]
    public void NoticeTypes_All_IgnoresCase(string value)
    {
        Assert.True(NoticeTypes.All.Contains(value));
    }

    [Fact]
    public void NoticeTypes_All_RejectsUnknownTypes()
    {
        Assert.False(NoticeTypes.All.Contains("ANNOUNCEMENT"));
    }

    [Fact]
    public void NoticeAuditActions_MatchTheDatabaseConstraint()
    {
        Assert.Equal("PUBLISH", NoticeAuditActions.Publish);
        Assert.Equal("UPDATE", NoticeAuditActions.Update);
        Assert.Equal("DELETE", NoticeAuditActions.Delete);
        Assert.Equal("ARCHIVE", NoticeAuditActions.Archive);
    }

    [Fact]
    public void NoticeRoles_MatchTheRoleNamesIssuedByIdentityService()
    {
        Assert.Equal("STUDENT", NoticeRoles.Student);
        Assert.Equal("WARDEN", NoticeRoles.Warden);
        Assert.Equal("HOSTEL_MASTER", NoticeRoles.HostelMaster);
        Assert.Equal("ADMIN", NoticeRoles.Admin);
        Assert.Equal("SYSTEM", NoticeRoles.System);
    }

    // ---------- RequireRoleAttribute ----------

    [Fact]
    public void RequireRoleAttribute_WithNoRoles_Throws()
    {
        Assert.Throws<ArgumentException>(() => new RequireRoleAttribute());
    }

    [Fact]
    public void RequireRoleAttribute_KeepsTheRolesItWasGiven()
    {
        var attribute = new RequireRoleAttribute("WARDEN", "ADMIN");

        Assert.Equal(new[] { "WARDEN", "ADMIN" }, attribute.AllowedRoles);
    }

    // ---------- RoleAuthorizationMiddleware ----------

    [Fact]
    public async Task Middleware_LetsAnEndpointWithoutARoleRequirementThrough()
    {
        var (reached, _) = await InvokeAsync(requirement: null, role: null);

        Assert.True(reached);
    }

    [Fact]
    public async Task Middleware_Returns401_ForAnAnonymousCaller()
    {
        var (reached, status) = await InvokeAsync(
            new RequireRoleAttribute("ADMIN"),
            role: null);

        Assert.False(reached);
        Assert.Equal(StatusCodes.Status401Unauthorized, status);
    }

    [Fact]
    public async Task Middleware_Returns403_ForAStudentOnAStaffEndpoint()
    {
        var (reached, status) = await InvokeAsync(
            new RequireRoleAttribute("WARDEN", "HOSTEL_MASTER", "ADMIN"),
            "STUDENT");

        Assert.False(reached);
        Assert.Equal(StatusCodes.Status403Forbidden, status);
    }

    [Theory]
    [InlineData("WARDEN")]
    [InlineData("HOSTEL_MASTER")]
    [InlineData("ADMIN")]
    public async Task Middleware_AllowsEachStaffRoleOnAStaffEndpoint(string role)
    {
        var (reached, _) = await InvokeAsync(
            new RequireRoleAttribute("WARDEN", "HOSTEL_MASTER", "ADMIN"),
            role);

        Assert.True(reached);
    }

    private static async Task<(bool Reached, int Status)> InvokeAsync(
        RequireRoleAttribute? requirement,
        string? role)
    {
        bool reached = false;

        var middleware = new RoleAuthorizationMiddleware(_ =>
        {
            reached = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var metadata = requirement is null
            ? new EndpointMetadataCollection()
            : new EndpointMetadataCollection(requirement);

        context.SetEndpoint(
            new Endpoint(_ => Task.CompletedTask, metadata, "notice-test-endpoint"));

        if (role is not null)
        {
            context.User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.Role, role) },
                    "Test"));
        }

        await middleware.InvokeAsync(context);

        return (reached, context.Response.StatusCode);
    }
}
