using System.Security.Claims;
using LeaveService.Controllers;
using LeaveService.DTOs;
using LeaveService.Exceptions;
using LeaveService.Models;
using LeaveService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LeaveService.Tests;

/// <summary>
/// Checks that the controllers turn service results and failures
/// into the right HTTP status codes and pass the right user on.
/// </summary>
public class LeaveControllerTests
{
    [Fact]
    public async Task Submit_WithValidRequest_Returns201WithTheCreatedRequest()
    {
        var service = new StubLeaveRequestService
        {
            SubmitResult = new LeaveRequestResponse { LeaveRequestId = 5, Status = "PENDING" }
        };
        var controller = CreateStudentController(service, "7", "student7");

        var result = await controller.Submit(new SubmitLeaveRequest());

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, created.StatusCode);
        Assert.Equal((ulong)7, service.LastStudentUserId);
        Assert.Equal("student7", service.LastStudentUsername);
    }

    [Fact]
    public async Task Submit_WhenValidationFails_Returns400WithFieldErrors()
    {
        var service = new StubLeaveRequestService
        {
            SubmitException = new LeaveRequestValidationException(
                new Dictionary<string, string[]> { ["reason"] = new[] { "Reason is required." } })
        };
        var controller = CreateStudentController(service, "7", "student7");

        var result = await controller.Submit(new SubmitLeaveRequest());

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var body = Assert.IsType<ValidationErrorResponse>(bad.Value);
        Assert.True(body.Errors.ContainsKey("reason"));
    }

    [Fact]
    public async Task Submit_WithoutAUserIdClaim_Returns401()
    {
        var controller = CreateStudentController(
            new StubLeaveRequestService(),
            userId: null,
            username: "student7");

        var result = await controller.Submit(new SubmitLeaveRequest());

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetMyRequest_ForAnotherStudentsRequest_Returns404()
    {
        var controller = CreateStudentController(
            new StubLeaveRequestService { GetOneResult = null },
            "7",
            "student7");

        var result = await controller.GetMyRequest(99);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Theory]
    [InlineData(LeaveRoles.Warden)]
    [InlineData(LeaveRoles.HostelMaster)]
    public async Task Decide_PassesTheActingStaffMemberAndRoleToTheService(
        string role)
    {
        var service = new StubReviewService();
        var controller = CreateActionsController(service, "20", role);

        var result = await controller.Decide(
            3,
            new DecideLeaveRequest { Decision = "APPROVE", Reason = "OK" });

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal((ulong)3, service.LastLeaveRequestId);
        Assert.Equal((ulong)20, service.LastActorUserId);
        Assert.Equal(role, service.LastActorRole);
    }

    [Fact]
    public async Task Decide_WhenRequestIsNotPending_Returns409Conflict()
    {
        var service = new StubReviewService
        {
            Failure = new InvalidLeaveStatusException("Only pending leave requests can be approved or rejected.")
        };
        var controller = CreateActionsController(service, "20", LeaveRoles.Warden);

        var result = await controller.Decide(
            3,
            new DecideLeaveRequest { Decision = "APPROVE", Reason = "OK" });

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(409, conflict.StatusCode);
    }

    [Fact]
    public async Task Decide_WhenReasonIsMissing_Returns400()
    {
        var service = new StubReviewService
        {
            Failure = new LeaveRequestValidationException(
                new Dictionary<string, string[]> { ["reason"] = new[] { "A reason for the decision is required." } })
        };
        var controller = CreateActionsController(service, "20", LeaveRoles.Warden);

        var result = await controller.Decide(3, new DecideLeaveRequest());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task MovementActions_WhenRequestDoesNotExist_Return404()
    {
        var service = new StubReviewService
        {
            Failure = new LeaveRequestNotFoundException()
        };
        var controller = CreateActionsController(service, "20", LeaveRoles.Warden);

        Assert.IsType<NotFoundObjectResult>(await controller.RecordDeparture(404));
        Assert.IsType<NotFoundObjectResult>(await controller.RecordReturn(404));
    }

    [Fact]
    public async Task Actions_WithoutAUserIdClaim_Return401()
    {
        var controller = CreateActionsController(
            new StubReviewService(),
            userId: null,
            role: LeaveRoles.Warden);

        Assert.IsType<UnauthorizedObjectResult>(await controller.RecordDeparture(1));
    }

    [Fact]
    public async Task Report_WithBadFilter_Returns400()
    {
        var service = new StubReviewService
        {
            Failure = new LeaveRequestValidationException(
                new Dictionary<string, string[]> { ["status"] = new[] { "Invalid." } })
        };
        var controller = new StaffLeaveRequestsController(service);

        var result = await controller.GetReport(new LeaveReportFilter());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Report_WithValidFilter_Returns200()
    {
        var controller = new StaffLeaveRequestsController(new StubReviewService());

        var result = await controller.GetReport(new LeaveReportFilter());

        Assert.IsType<OkObjectResult>(result);
    }

    private static StudentLeaveRequestsController CreateStudentController(
        ILeaveRequestService service,
        string? userId,
        string username)
    {
        return new StudentLeaveRequestsController(service)
        {
            ControllerContext = CreateContext(userId, LeaveRoles.Student, username)
        };
    }

    private static StaffLeaveActionsController CreateActionsController(
        ILeaveReviewService service,
        string? userId,
        string role)
    {
        return new StaffLeaveActionsController(service)
        {
            ControllerContext = CreateContext(userId, role, "staff")
        };
    }

    private static ControllerContext CreateContext(
        string? userId,
        string role,
        string username)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, role),
            new("unique_name", username)
        };

        if (userId is not null)
        {
            claims.Add(new Claim("sub", userId));
        }

        var identity = new ClaimsIdentity(
            claims,
            authenticationType: "Test",
            nameType: "unique_name",
            roleType: ClaimTypes.Role);

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
    }

    private sealed class StubLeaveRequestService : ILeaveRequestService
    {
        public LeaveRequestResponse SubmitResult { get; set; } = new();

        public Exception? SubmitException { get; set; }

        public LeaveRequestResponse? GetOneResult { get; set; }

        public ulong? LastStudentUserId { get; private set; }

        public string? LastStudentUsername { get; private set; }

        public Task<LeaveRequestResponse> SubmitAsync(
            SubmitLeaveRequest request,
            ulong studentUserId,
            string studentUsername)
        {
            if (SubmitException is not null)
            {
                throw SubmitException;
            }

            LastStudentUserId = studentUserId;
            LastStudentUsername = studentUsername;

            return Task.FromResult(SubmitResult);
        }

        public Task<IReadOnlyList<LeaveRequestResponse>> GetMyRequestsAsync(
            ulong studentUserId)
        {
            return Task.FromResult<IReadOnlyList<LeaveRequestResponse>>(
                new List<LeaveRequestResponse>());
        }

        public Task<LeaveRequestResponse?> GetMyRequestAsync(
            ulong leaveRequestId,
            ulong studentUserId)
        {
            return Task.FromResult(GetOneResult);
        }
    }

    private sealed class StubReviewService : ILeaveReviewService
    {
        public Exception? Failure { get; set; }

        public ulong? LastLeaveRequestId { get; private set; }

        public ulong? LastActorUserId { get; private set; }

        public string? LastActorRole { get; private set; }

        public Task<LeaveReportResponse> GetReportAsync(LeaveReportFilter filter)
        {
            if (Failure is not null)
            {
                throw Failure;
            }

            return Task.FromResult(new LeaveReportResponse());
        }

        public Task<LeaveRequestResponse> DecideAsync(
            ulong leaveRequestId,
            DecideLeaveRequest request,
            ulong actorUserId,
            string actorRole)
        {
            return Record(leaveRequestId, actorUserId, actorRole);
        }

        public Task<LeaveRequestResponse> RecordDepartureAsync(
            ulong leaveRequestId,
            ulong actorUserId,
            string actorRole)
        {
            return Record(leaveRequestId, actorUserId, actorRole);
        }

        public Task<LeaveRequestResponse> RecordReturnAsync(
            ulong leaveRequestId,
            ulong actorUserId,
            string actorRole)
        {
            return Record(leaveRequestId, actorUserId, actorRole);
        }

        private Task<LeaveRequestResponse> Record(
            ulong leaveRequestId,
            ulong actorUserId,
            string actorRole)
        {
            if (Failure is not null)
            {
                throw Failure;
            }

            LastLeaveRequestId = leaveRequestId;
            LastActorUserId = actorUserId;
            LastActorRole = actorRole;

            return Task.FromResult(new LeaveRequestResponse { LeaveRequestId = leaveRequestId });
        }
    }
}
