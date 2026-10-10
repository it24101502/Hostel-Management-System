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
/// Checks that StaffComplaintsController maps service results and
/// failures to HTTP status codes and passes the acting user and role on.
/// </summary>
public class StaffComplaintsControllerTests
{
    private readonly IStaffComplaintService _service =
        Substitute.For<IStaffComplaintService>();

    private StaffComplaintsController CreateController(
        string? userId = "20",
        string role = ComplaintRoles.Warden)
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, role) };

        if (userId is not null)
        {
            claims.Add(new Claim("sub", userId));
        }

        var identity = new ClaimsIdentity(
            claims, "Test", "unique_name", ClaimTypes.Role);

        return new StaffComplaintsController(_service)
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

    // ---------- list ----------

    [Fact]
    public async Task GetComplaints_ReturnsOkWithTheFilteredList()
    {
        IReadOnlyList<ComplaintResponse> complaints =
            new[] { new ComplaintResponse { ComplaintId = 1 } };

        _service.GetComplaintsAsync("OPEN", "NOISE").Returns(complaints);

        var result = await CreateController().GetComplaints("OPEN", "NOISE");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(complaints, ok.Value);
    }

    [Fact]
    public async Task GetComplaints_WithInvalidFilter_Returns400WithFieldErrors()
    {
        _service.GetComplaintsAsync("nonsense", null)
            .ThrowsAsync(ValidationError("status"));

        var result = await CreateController().GetComplaints("nonsense", null);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var body = Assert.IsType<ValidationErrorResponse>(bad.Value);
        Assert.True(body.Errors.ContainsKey("status"));
    }

    // ---------- report ----------

    [Fact]
    public async Task GetReport_ReturnsOkWithTheReport()
    {
        var report = new ComplaintReportResponse();
        _service.GetReportAsync(null, "PLUMBING").Returns(report);

        var result = await CreateController().GetReport(null, "PLUMBING");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(report, ok.Value);
    }

    [Fact]
    public async Task GetReport_WithInvalidFilter_Returns400()
    {
        _service.GetReportAsync("bad", null)
            .ThrowsAsync(ValidationError("status"));

        var result = await CreateController().GetReport("bad", null);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DownloadReportCsv_ReturnsACsvFileWithADatedName()
    {
        byte[] bytes = { 1, 2, 3 };
        _service.GenerateReportCsvAsync("OPEN", null).Returns(bytes);

        var result = await CreateController().DownloadReportCsv("OPEN", null);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv; charset=utf-8", file.ContentType);
        Assert.Equal(bytes, file.FileContents);
        Assert.StartsWith("complaint-report-", file.FileDownloadName);
        Assert.EndsWith(".csv", file.FileDownloadName);
    }

    [Fact]
    public async Task DownloadReportCsv_WithInvalidFilter_Returns400()
    {
        _service.GenerateReportCsvAsync(null, "bad")
            .ThrowsAsync(ValidationError("category"));

        var result = await CreateController().DownloadReportCsv(null, "bad");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ---------- assign ----------

    [Fact]
    public async Task Assign_PassesTheActingStaffMemberAndRoleToTheService()
    {
        var request = new AssignComplaintRequest { AssignedToUserId = 31 };

        _service.AssignAsync(3UL, request, 20UL, ComplaintRoles.Warden)
            .Returns(new ComplaintResponse { ComplaintId = 3 });

        var result = await CreateController().Assign(3, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal((ulong)3, ((ComplaintResponse)ok.Value!).ComplaintId);
    }

    [Fact]
    public async Task Assign_WithoutAUserIdClaim_Returns401AndDoesNotCallService()
    {
        var result = await CreateController(userId: null)
            .Assign(3, new AssignComplaintRequest());

        Assert.IsType<UnauthorizedObjectResult>(result);
        await _service.DidNotReceive().AssignAsync(
            Arg.Any<ulong>(),
            Arg.Any<AssignComplaintRequest>(),
            Arg.Any<ulong>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task Assign_WhenComplaintDoesNotExist_Returns404()
    {
        _service.AssignAsync(
                404UL,
                Arg.Any<AssignComplaintRequest>(),
                20UL,
                Arg.Any<string>())
            .ThrowsAsync(new ComplaintNotFoundException());

        var result = await CreateController()
            .Assign(404, new AssignComplaintRequest());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Assign_WithInvalidAssignee_Returns400()
    {
        _service.AssignAsync(
                3UL,
                Arg.Any<AssignComplaintRequest>(),
                20UL,
                Arg.Any<string>())
            .ThrowsAsync(ValidationError("assignedToUserId"));

        var result = await CreateController()
            .Assign(3, new AssignComplaintRequest { AssignedToUserId = 0 });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ---------- change status ----------

    [Fact]
    public async Task ChangeStatus_PassesTheActingStaffMemberAndRoleToTheService()
    {
        var request = new UpdateComplaintStatusRequest { Status = "RESOLVED" };

        _service.ChangeStatusAsync(3UL, request, 20UL, ComplaintRoles.Warden)
            .Returns(new ComplaintResponse { ComplaintId = 3 });

        var result = await CreateController().ChangeStatus(3, request);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ChangeStatus_WhenStatusIsInvalid_Returns400()
    {
        _service.ChangeStatusAsync(
                3UL,
                Arg.Any<UpdateComplaintStatusRequest>(),
                20UL,
                Arg.Any<string>())
            .ThrowsAsync(ValidationError("status"));

        var result = await CreateController()
            .ChangeStatus(3, new UpdateComplaintStatusRequest());

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<ValidationErrorResponse>(bad.Value);
    }

    [Fact]
    public async Task ChangeStatus_WhenComplaintDoesNotExist_Returns404()
    {
        _service.ChangeStatusAsync(
                404UL,
                Arg.Any<UpdateComplaintStatusRequest>(),
                20UL,
                Arg.Any<string>())
            .ThrowsAsync(new ComplaintNotFoundException());

        var result = await CreateController()
            .ChangeStatus(404, new UpdateComplaintStatusRequest());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task ChangeStatus_WhenStatusIsUnchangedOrRaced_Returns409()
    {
        _service.ChangeStatusAsync(
                3UL,
                Arg.Any<UpdateComplaintStatusRequest>(),
                20UL,
                Arg.Any<string>())
            .ThrowsAsync(
                new InvalidComplaintStatusException("The complaint is already open."));

        var result = await CreateController()
            .ChangeStatus(3, new UpdateComplaintStatusRequest());

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(409, conflict.StatusCode);
    }

    [Fact]
    public async Task ChangeStatus_WithoutAUserIdClaim_Returns401()
    {
        var result = await CreateController(userId: null)
            .ChangeStatus(3, new UpdateComplaintStatusRequest());

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    // ---------- acting role ----------

    [Theory]
    [InlineData(ComplaintRoles.Warden)]
    [InlineData(ComplaintRoles.HostelMaster)]
    [InlineData(ComplaintRoles.Admin)]
    public async Task Actions_ReportTheCallersRoleToTheService(string role)
    {
        string? receivedRole = null;

        _service.ChangeStatusAsync(
                Arg.Any<ulong>(),
                Arg.Any<UpdateComplaintStatusRequest>(),
                Arg.Any<ulong>(),
                Arg.Do<string>(value => receivedRole = value))
            .Returns(new ComplaintResponse());

        await CreateController(role: role)
            .ChangeStatus(1, new UpdateComplaintStatusRequest());

        Assert.Equal(role, receivedRole);
    }
}
