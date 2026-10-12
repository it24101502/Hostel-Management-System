using System.Reflection;
using System.Security.Claims;
using ComplaintService.Authorization;
using ComplaintService.Controllers;
using ComplaintService.DTOs;
using ComplaintService.Exceptions;
using ComplaintService.Middleware;
using ComplaintService.Models;
using ComplaintService.Repositories;
using ComplaintService.Services;
using ComplaintService.Tests.TestDoubles;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace ComplaintService.Tests;

/// <summary>
/// Covers the parts of StudentComplaintService, the role declarations,
/// the middleware and the shared constants that the other test classes
/// do not.
/// </summary>
public class StudentComplaintServiceAdditionalTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 5, 10, 0, 0, TimeSpan.Zero);

    private static StudentComplaintService CreateService(
        IComplaintRepository repository) =>
        new(repository, new FixedTimeProvider(Now));

    // ---------- listing ----------

    [Fact]
    public async Task GetMyComplaintsAsync_ReturnsOnlyTheStudentsComplaintsNewestFirst()
    {
        var repository = new FakeComplaintRepository();
        repository.Seed(7);
        repository.Seed(8);
        repository.Seed(7);

        var result = await CreateService(repository).GetMyComplaintsAsync(7);

        Assert.Equal(2, result.Count);
        Assert.All(result, c => Assert.Equal((ulong)7, c.StudentUserId));
        Assert.True(result[0].ComplaintId > result[1].ComplaintId);
    }

    [Fact]
    public async Task GetMyComplaintsAsync_WhenStudentHasNone_ReturnsEmptyList()
    {
        var repository = new FakeComplaintRepository();
        repository.Seed(8);

        var result = await CreateService(repository).GetMyComplaintsAsync(7);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMyComplaintsAsync_MapsEveryFieldOfTheComplaint()
    {
        var repository = new FakeComplaintRepository();
        var complaint = repository.Seed(7, ComplaintStatuses.Resolved, "NOISE");
        complaint.AssignedToUserId = 31;
        complaint.AssignedAt = new DateTime(2026, 10, 2, 9, 0, 0, DateTimeKind.Utc);

        var result = Assert.Single(
            await CreateService(repository).GetMyComplaintsAsync(7));

        Assert.Equal(complaint.ComplaintId, result.ComplaintId);
        Assert.Equal("student7", result.StudentUsername);
        Assert.Equal("NOISE", result.Category);
        Assert.Equal("Seeded complaint", result.Description);
        Assert.Equal(ComplaintStatuses.Resolved, result.Status);
        Assert.Equal((ulong)31, result.AssignedToUserId);
        Assert.Equal(complaint.AssignedAt, result.AssignedAt);
        Assert.Equal(complaint.ResolvedAt, result.ResolvedAt);
    }

    [Fact]
    public async Task GetMyComplaintAsync_ForAnOwnComplaint_ReturnsIt()
    {
        var repository = new FakeComplaintRepository();
        var own = repository.Seed(7);

        var result = await CreateService(repository)
            .GetMyComplaintAsync(own.ComplaintId, 7);

        Assert.NotNull(result);
        Assert.Equal(own.ComplaintId, result.ComplaintId);
    }

    [Fact]
    public async Task GetMyComplaintAsync_WhenComplaintDoesNotExist_ReturnsNull()
    {
        var result = await CreateService(new FakeComplaintRepository())
            .GetMyComplaintAsync(404, 7);

        Assert.Null(result);
    }

    // ---------- submission edge cases ----------

    [Fact]
    public async Task SubmitAsync_WithDescriptionAtMaximumLength_IsAccepted()
    {
        var repository = new FakeComplaintRepository();

        var result = await CreateService(repository).SubmitAsync(
            new SubmitComplaintRequest
            {
                Category = "OTHER",
                Description = new string(
                    'a', StudentComplaintService.MaxDescriptionLength)
            },
            7,
            "student7");

        Assert.Equal(
            StudentComplaintService.MaxDescriptionLength,
            result.Description.Length);
    }

    [Fact]
    public async Task SubmitAsync_WithBothFieldsInvalid_ReportsBothErrors()
    {
        var exception = await Assert.ThrowsAsync<ComplaintValidationException>(
            () => CreateService(new FakeComplaintRepository()).SubmitAsync(
                new SubmitComplaintRequest
                {
                    Category = "NOT_A_CATEGORY",
                    Description = "   "
                },
                7,
                "student7"));

        Assert.Equal(2, exception.Errors.Count);
        Assert.True(exception.Errors.ContainsKey("category"));
        Assert.True(exception.Errors.ContainsKey("description"));
    }

    [Fact]
    public async Task SubmitAsync_SavesTheStudentIdentityFromTheCaller_NotTheRequest()
    {
        var repository = new FakeComplaintRepository();

        await CreateService(repository).SubmitAsync(
            new SubmitComplaintRequest
            {
                Category = "FOOD",
                Description = "Cold meals."
            },
            studentUserId: 42,
            studentUsername: "student42");

        Assert.Equal((ulong)42, repository.LastNewComplaint!.StudentUserId);
        Assert.Equal("student42", repository.LastNewComplaint.StudentUsername);
    }

    [Theory]
    [InlineData("MAINTENANCE")]
    [InlineData("ELECTRICAL")]
    [InlineData("PLUMBING")]
    [InlineData("CLEANLINESS")]
    [InlineData("SECURITY")]
    [InlineData("FOOD")]
    [InlineData("NOISE")]
    [InlineData("OTHER")]
    public async Task SubmitAsync_AcceptsEveryAllowedCategory(string category)
    {
        var result = await CreateService(new FakeComplaintRepository())
            .SubmitAsync(
                new SubmitComplaintRequest
                {
                    Category = category,
                    Description = "Something is wrong."
                },
                7,
                "student7");

        Assert.Equal(category, result.Category);
    }

    // ---------- notifications ----------

    [Fact]
    public async Task GetNotificationsByStudentIdAsync_ForwardsToTheRepository()
    {
        var repository = Substitute.For<IComplaintRepository>();

        IReadOnlyList<StudentNotificationResponse> notifications =
            new[] { new StudentNotificationResponse { Message = "Updated." } };

        repository.GetNotificationsByStudentIdAsync(7UL).Returns(notifications);

        var result = await CreateService(repository)
            .GetNotificationsByStudentIdAsync(7);

        Assert.Same(notifications, result);
    }

    [Fact]
    public async Task MarkNotificationReadAsync_PassesTheCurrentUtcTimeToTheRepository()
    {
        var repository = Substitute.For<IComplaintRepository>();

        repository.MarkNotificationReadAsync(
                5UL, 7UL, Arg.Any<DateTime>())
            .Returns(true);

        bool found = await CreateService(repository)
            .MarkNotificationReadAsync(5, 7);

        Assert.True(found);

        await repository.Received(1).MarkNotificationReadAsync(
            5UL, 7UL, Now.UtcDateTime);
    }

    // ---------- shared constants ----------

    [Fact]
    public void ComplaintCategories_MatchTheDatabaseConstraintList()
    {
        Assert.Equal(
            new[]
            {
                "MAINTENANCE", "ELECTRICAL", "PLUMBING", "CLEANLINESS",
                "SECURITY", "FOOD", "NOISE", "OTHER"
            },
            ComplaintCategories.All.ToArray());
    }

    [Fact]
    public void ComplaintCategories_HaveNoDuplicates()
    {
        Assert.Equal(
            ComplaintCategories.All.Count,
            ComplaintCategories.All.Distinct().Count());
    }

    [Fact]
    public void ComplaintStatuses_MatchTheDatabaseConstraintValues()
    {
        Assert.Equal("OPEN", ComplaintStatuses.Open);
        Assert.Equal("IN_PROGRESS", ComplaintStatuses.InProgress);
        Assert.Equal("RESOLVED", ComplaintStatuses.Resolved);
    }

    [Fact]
    public void ComplaintAuditActions_MatchTheDatabaseConstraintValues()
    {
        Assert.Equal("SUBMIT", ComplaintAuditActions.Submit);
        Assert.Equal("ASSIGN", ComplaintAuditActions.Assign);
        Assert.Equal("STATUS_CHANGE", ComplaintAuditActions.StatusChange);
    }

    [Fact]
    public void ComplaintRoles_MatchTheRoleNamesIssuedByIdentityService()
    {
        Assert.Equal("STUDENT", ComplaintRoles.Student);
        Assert.Equal("WARDEN", ComplaintRoles.Warden);
        Assert.Equal("HOSTEL_MASTER", ComplaintRoles.HostelMaster);
        Assert.Equal("ADMIN", ComplaintRoles.Admin);
        Assert.Equal("SYSTEM", ComplaintRoles.System);
    }

    // ---------- declared roles ----------

    [Fact]
    public void StudentController_RequiresOnlyTheStudentRole()
    {
        Assert.Equal(
            new[] { ComplaintRoles.Student },
            GetAllowedRoles(typeof(StudentComplaintsController)));
    }

    [Fact]
    public void StaffController_RequiresWardenHostelMasterOrAdmin()
    {
        Assert.Equal(
            new[]
            {
                ComplaintRoles.Warden,
                ComplaintRoles.HostelMaster,
                ComplaintRoles.Admin
            },
            GetAllowedRoles(typeof(StaffComplaintsController)));
    }

    private static string[] GetAllowedRoles(Type controllerType)
    {
        var attribute =
            controllerType.GetCustomAttribute<RequireRoleAttribute>();

        Assert.NotNull(attribute);

        return attribute.AllowedRoles.ToArray();
    }

    // ---------- RequireRoleAttribute and middleware ----------

    [Fact]
    public void RequireRoleAttribute_WithNoRoles_Throws()
    {
        Assert.Throws<ArgumentException>(() => new RequireRoleAttribute());
    }

    [Fact]
    public async Task Middleware_LetsAnEndpointWithoutARoleRequirementThrough()
    {
        bool reached = false;

        var middleware = new RoleAuthorizationMiddleware(_ =>
        {
            reached = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();

        context.SetEndpoint(
            new Endpoint(
                _ => Task.CompletedTask,
                new EndpointMetadataCollection(),
                "public-endpoint"));

        await middleware.InvokeAsync(context);

        Assert.True(reached);
    }

    [Fact]
    public async Task Middleware_Rejects401WithoutCallingTheEndpoint_ForAnonymousUsers()
    {
        var (reached, status) = await InvokeMiddlewareAsync(
            new RequireRoleAttribute(ComplaintRoles.Student),
            role: null);

        Assert.False(reached);
        Assert.Equal(StatusCodes.Status401Unauthorized, status);
    }

    [Fact]
    public async Task Middleware_Rejects403_WhenTheRoleIsNotAllowed()
    {
        var (reached, status) = await InvokeMiddlewareAsync(
            new RequireRoleAttribute(ComplaintRoles.Admin),
            ComplaintRoles.Student);

        Assert.False(reached);
        Assert.Equal(StatusCodes.Status403Forbidden, status);
    }

    [Fact]
    public async Task Middleware_AllowsAnyOneOfSeveralAllowedRoles()
    {
        var (reached, _) = await InvokeMiddlewareAsync(
            new RequireRoleAttribute(
                ComplaintRoles.Warden,
                ComplaintRoles.HostelMaster),
            ComplaintRoles.HostelMaster);

        Assert.True(reached);
    }

    private static async Task<(bool Reached, int Status)>
        InvokeMiddlewareAsync(RequireRoleAttribute requirement, string? role)
    {
        bool reached = false;

        var middleware = new RoleAuthorizationMiddleware(_ =>
        {
            reached = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        context.SetEndpoint(
            new Endpoint(
                _ => Task.CompletedTask,
                new EndpointMetadataCollection(requirement),
                "protected-endpoint"));

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
