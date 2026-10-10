using System.Security.Claims;
using ComplaintService.Controllers;
using ComplaintService.DTOs;
using ComplaintService.Exceptions;
using ComplaintService.Models;
using ComplaintService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ComplaintService.Tests;

/// <summary>
/// Checks that StudentComplaintsController turns service results and
/// failures into the right HTTP status codes and passes the right user on.
/// </summary>
public class StudentComplaintsControllerTests
{
    private readonly IStudentComplaintService _service =
        Substitute.For<IStudentComplaintService>();

    private StudentComplaintsController CreateController(
        string? userId = "7",
        string? username = "student7",
        string? nameIdentifier = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, ComplaintRoles.Student)
        };

        if (userId is not null)
        {
            claims.Add(new Claim("sub", userId));
        }

        if (nameIdentifier is not null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, nameIdentifier));
        }

        if (username is not null)
        {
            claims.Add(new Claim("unique_name", username));
        }

        var identity = new ClaimsIdentity(
            claims, "Test", "unique_name", ClaimTypes.Role);

        return new StudentComplaintsController(_service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            }
        };
    }

    private static ComplaintValidationException ValidationError(string field) =>
        new(new Dictionary<string, string[]>
        {
            [field] = new[] { "Invalid." }
        });

    // ---------- GET /api/student/complaints ----------

    [Fact]
    public async Task GetMyComplaints_ReturnsOkWithTheStudentsComplaints()
    {
        IReadOnlyList<ComplaintResponse> complaints =
            new[] { new ComplaintResponse { ComplaintId = 1 } };

        _service.GetMyComplaintsAsync(7UL).Returns(complaints);

        var result = await CreateController().GetMyComplaints();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(complaints, ok.Value);
    }

    [Fact]
    public async Task GetMyComplaints_WithoutAUserIdClaim_Returns401AndDoesNotCallService()
    {
        var result = await CreateController(userId: null).GetMyComplaints();

        Assert.IsType<UnauthorizedObjectResult>(result);
        await _service.DidNotReceive().GetMyComplaintsAsync(Arg.Any<ulong>());
    }

    [Fact]
    public async Task GetMyComplaints_WithANonNumericUserId_Returns401()
    {
        var result = await CreateController(userId: "abc").GetMyComplaints();

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetMyComplaints_FallsBackToTheNameIdentifierClaim()
    {
        IReadOnlyList<ComplaintResponse> complaints =
            Array.Empty<ComplaintResponse>();

        _service.GetMyComplaintsAsync(9UL).Returns(complaints);

        var result = await CreateController(
            userId: null,
            nameIdentifier: "9").GetMyComplaints();

        Assert.IsType<OkObjectResult>(result);
        await _service.Received(1).GetMyComplaintsAsync(9UL);
    }

    // ---------- GET /api/student/complaints/{id} ----------

    [Fact]
    public async Task GetMyComplaint_ForAnOwnComplaint_ReturnsOk()
    {
        _service.GetMyComplaintAsync(5UL, 7UL)
            .Returns(new ComplaintResponse { ComplaintId = 5 });

        var result = await CreateController().GetMyComplaint(5);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal((ulong)5, ((ComplaintResponse)ok.Value!).ComplaintId);
    }

    [Fact]
    public async Task GetMyComplaint_WhenNotFoundOrNotOwned_Returns404()
    {
        _service.GetMyComplaintAsync(5UL, 7UL)
            .Returns((ComplaintResponse?)null);

        var result = await CreateController().GetMyComplaint(5);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetMyComplaint_WithoutAUserIdClaim_Returns401()
    {
        var result = await CreateController(userId: null).GetMyComplaint(5);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    // ---------- POST /api/student/complaints ----------

    [Fact]
    public async Task Submit_WithValidRequest_Returns201PointingAtTheNewComplaint()
    {
        var request = new SubmitComplaintRequest
        {
            Category = "PLUMBING",
            Description = "Leaking tap."
        };

        _service.SubmitAsync(request, 7UL, "student7")
            .Returns(new ComplaintResponse { ComplaintId = 12 });

        var result = await CreateController().Submit(request);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, created.StatusCode);
        Assert.Equal(
            nameof(StudentComplaintsController.GetMyComplaint),
            created.ActionName);
        Assert.Equal((ulong)12, created.RouteValues!["complaintId"]);
    }

    [Fact]
    public async Task Submit_WhenValidationFails_Returns400WithFieldErrors()
    {
        var request = new SubmitComplaintRequest();

        _service.SubmitAsync(request, 7UL, "student7")
            .ThrowsAsync(ValidationError("description"));

        var result = await CreateController().Submit(request);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var body = Assert.IsType<ValidationErrorResponse>(bad.Value);
        Assert.True(body.Errors.ContainsKey("description"));
    }

    [Fact]
    public async Task Submit_WithoutAUserIdClaim_Returns401AndDoesNotCallService()
    {
        var result = await CreateController(userId: null)
            .Submit(new SubmitComplaintRequest());

        Assert.IsType<UnauthorizedObjectResult>(result);
        await _service.DidNotReceive().SubmitAsync(
            Arg.Any<SubmitComplaintRequest>(),
            Arg.Any<ulong>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task Submit_WithoutAUsernameClaim_Returns401AndDoesNotCallService()
    {
        var result = await CreateController(username: null)
            .Submit(new SubmitComplaintRequest());

        Assert.IsType<UnauthorizedObjectResult>(result);
        await _service.DidNotReceive().SubmitAsync(
            Arg.Any<SubmitComplaintRequest>(),
            Arg.Any<ulong>(),
            Arg.Any<string>());
    }

    // ---------- notifications ----------

    [Fact]
    public async Task GetStudentNotifications_ReturnsOkForTheSignedInStudent()
    {
        IReadOnlyList<StudentNotificationResponse> notifications =
            new[] { new StudentNotificationResponse { Message = "Updated." } };

        _service.GetNotificationsByStudentIdAsync(7UL).Returns(notifications);

        var result = await CreateController().GetStudentNotifications();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(notifications, ok.Value);
    }

    [Fact]
    public async Task GetStudentNotifications_WithoutAUserIdClaim_Returns401()
    {
        var result = await CreateController(userId: null)
            .GetStudentNotifications();

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task MarkNotificationRead_ForAnOwnNotification_Returns204()
    {
        _service.MarkNotificationReadAsync(3UL, 7UL).Returns(true);

        var result = await CreateController().MarkNotificationRead(3);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task MarkNotificationRead_WhenMissingOrForeign_Returns404()
    {
        _service.MarkNotificationReadAsync(3UL, 7UL).Returns(false);

        var result = await CreateController().MarkNotificationRead(3);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task MarkNotificationRead_WithoutAUserIdClaim_Returns401()
    {
        var result = await CreateController(userId: null)
            .MarkNotificationRead(3);

        Assert.IsType<UnauthorizedObjectResult>(result);
        await _service.DidNotReceive().MarkNotificationReadAsync(
            Arg.Any<ulong>(), Arg.Any<ulong>());
    }
}
