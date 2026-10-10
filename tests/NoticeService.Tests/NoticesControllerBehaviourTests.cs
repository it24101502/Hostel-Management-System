using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NoticeService.Controllers;
using NoticeService.DTOs;
using NoticeService.Repositories;
using Xunit;

namespace NoticeService.Tests;

/// <summary>
/// Covers the controller paths NoticesControllerTests does not: invalid
/// tokens, missing notices, role normalising, archived listing and the
/// block claim parsing.
/// </summary>
public class NoticesControllerBehaviourTests
{
    private readonly Mock<INoticeRepository> _repository = new();
    private readonly NoticesController _controller;

    public NoticesControllerBehaviourTests()
    {
        _controller = new NoticesController(_repository.Object);
    }

    private void SignIn(params Claim[] claims)
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            }
        };
    }

    private void SignInAsStaff(string role = "WARDEN", string userId = "10") =>
        SignIn(new Claim("sub", userId), new Claim(ClaimTypes.Role, role));

    private static DateOnly Tomorrow =>
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

    private static NoticeResponse Notice(ulong id = 1, ulong? block = null) =>
        new(id, "Title", "Content", "NOTICE", block, Tomorrow,
            false, null, 10, DateTime.UtcNow, DateTime.UtcNow);

    private static CreateNoticeRequest CreateRequest() =>
        new("Title", "Content", "NOTICE", null, Tomorrow);

    private static UpdateNoticeRequest UpdateRequest() =>
        new("Title", "Content", "NOTICE", null, Tomorrow);

    // ---------- create ----------

    [Fact]
    public async Task CreateNotice_WithoutAnyClaims_ReturnsUnauthorizedAndSavesNothing()
    {
        SignIn();

        var result = await _controller.CreateNotice(CreateRequest());

        Assert.IsType<UnauthorizedObjectResult>(result);

        _repository.Verify(
            r => r.CreateAsync(
                It.IsAny<CreateNoticeRequest>(),
                It.IsAny<ulong>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateNotice_WithARoleButNoUserId_ReturnsUnauthorized()
    {
        SignIn(new Claim(ClaimTypes.Role, "WARDEN"));

        var result = await _controller.CreateNotice(CreateRequest());

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task CreateNotice_WithANonNumericUserId_ReturnsUnauthorized()
    {
        SignInAsStaff(userId: "not-a-number");

        var result = await _controller.CreateNotice(CreateRequest());

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task CreateNotice_WithAUserIdButNoRole_ReturnsUnauthorized()
    {
        SignIn(new Claim("sub", "10"));

        var result = await _controller.CreateNotice(CreateRequest());

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task CreateNotice_PassesTheRoleToTheRepositoryInUpperCase()
    {
        var request = CreateRequest();

        _repository
            .Setup(r => r.CreateAsync(request, 10UL, "WARDEN"))
            .ReturnsAsync(Notice());

        SignInAsStaff(role: "warden");

        var result = await _controller.CreateNotice(request);

        Assert.IsType<CreatedAtActionResult>(result);
        _repository.Verify(r => r.CreateAsync(request, 10UL, "WARDEN"), Times.Once);
    }

    [Fact]
    public async Task CreateNotice_ReturnsTheNewNoticeAndItsLocation()
    {
        var request = CreateRequest();
        var created = Notice(id: 77);

        _repository
            .Setup(r => r.CreateAsync(request, 10UL, "ADMIN"))
            .ReturnsAsync(created);

        SignInAsStaff(role: "ADMIN");

        var result = await _controller.CreateNotice(request);

        var createdAt = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(NoticesController.GetNoticeById), createdAt.ActionName);
        Assert.Equal((ulong)77, createdAt.RouteValues!["id"]);
        Assert.Same(created, createdAt.Value);
    }

    // ---------- list ----------

    [Fact]
    public async Task GetAllNotices_ByDefault_ExcludesArchivedNotices()
    {
        _repository
            .Setup(r => r.GetAllAsync(false))
            .ReturnsAsync(new List<NoticeResponse> { Notice() });

        SignInAsStaff();

        var result = await _controller.GetAllNotices();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Single(Assert.IsAssignableFrom<IEnumerable<NoticeResponse>>(ok.Value));
        _repository.Verify(r => r.GetAllAsync(false), Times.Once);
    }

    [Fact]
    public async Task GetAllNotices_WhenAskedToIncludeArchived_PassesTheFlagOn()
    {
        _repository
            .Setup(r => r.GetAllAsync(true))
            .ReturnsAsync(new List<NoticeResponse>());

        SignInAsStaff();

        var result = await _controller.GetAllNotices(includeArchived: true);

        Assert.IsType<OkObjectResult>(result.Result);
        _repository.Verify(r => r.GetAllAsync(true), Times.Once);
    }

    // ---------- get by id ----------

    [Fact]
    public async Task GetNoticeById_WhenItExists_ReturnsIt()
    {
        var notice = Notice(id: 5);

        _repository.Setup(r => r.GetByIdAsync(5UL)).ReturnsAsync(notice);

        var result = await _controller.GetNoticeById(5UL);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(notice, ok.Value);
    }

    // ---------- update ----------

    [Fact]
    public async Task UpdateNotice_WithoutAnyClaims_ReturnsUnauthorizedAndUpdatesNothing()
    {
        SignIn();

        var result = await _controller.UpdateNotice(1UL, UpdateRequest());

        Assert.IsType<UnauthorizedObjectResult>(result);

        _repository.Verify(
            r => r.UpdateAsync(
                It.IsAny<ulong>(),
                It.IsAny<UpdateNoticeRequest>(),
                It.IsAny<ulong>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateNotice_WhenTheNoticeDoesNotExist_ReturnsNotFoundWithoutReloading()
    {
        var request = UpdateRequest();

        _repository
            .Setup(r => r.UpdateAsync(99UL, request, 10UL, "WARDEN"))
            .ReturnsAsync(false);

        SignInAsStaff();

        var result = await _controller.UpdateNotice(99UL, request);

        Assert.IsType<NotFoundObjectResult>(result);
        _repository.Verify(r => r.GetByIdAsync(It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public async Task UpdateNotice_ReturnsTheReloadedNotice()
    {
        var request = UpdateRequest();
        var updated = Notice(id: 4);

        _repository
            .Setup(r => r.UpdateAsync(4UL, request, 11UL, "HOSTEL_MASTER"))
            .ReturnsAsync(true);

        _repository.Setup(r => r.GetByIdAsync(4UL)).ReturnsAsync(updated);

        SignInAsStaff(role: "HOSTEL_MASTER", userId: "11");

        var result = await _controller.UpdateNotice(4UL, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(updated, ok.Value);
    }

    // ---------- delete ----------

    [Fact]
    public async Task DeleteNotice_WithoutAnyClaims_ReturnsUnauthorizedAndDeletesNothing()
    {
        SignIn();

        var result = await _controller.DeleteNotice(1UL);

        Assert.IsType<UnauthorizedObjectResult>(result);

        _repository.Verify(
            r => r.DeleteAsync(
                It.IsAny<ulong>(),
                It.IsAny<ulong>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteNotice_WhenTheNoticeDoesNotExist_ReturnsNotFound()
    {
        _repository
            .Setup(r => r.DeleteAsync(99UL, 10UL, "WARDEN"))
            .ReturnsAsync(false);

        SignInAsStaff();

        var result = await _controller.DeleteNotice(99UL);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ---------- student view ----------

    [Fact]
    public async Task GetMyNotices_WithANonNumericBlockClaim_FallsBackToGeneralNotices()
    {
        _repository
            .Setup(r => r.GetStudentNoticesAsync(0UL))
            .ReturnsAsync(new List<NoticeResponse>());

        SignIn(
            new Claim("sub", "201"),
            new Claim(ClaimTypes.Role, "STUDENT"),
            new Claim("hostel_block_id", "block-two"));

        var result = await _controller.GetMyNotices();

        Assert.IsType<OkObjectResult>(result.Result);
        _repository.Verify(r => r.GetStudentNoticesAsync(0UL), Times.Once);
    }

    [Fact]
    public async Task GetMyNotices_IgnoresAnyBlockSuppliedByTheCaller_UsingOnlyTheToken()
    {
        _repository
            .Setup(r => r.GetStudentNoticesAsync(3UL))
            .ReturnsAsync(new List<NoticeResponse> { Notice(block: 3) });

        SignIn(
            new Claim("sub", "201"),
            new Claim(ClaimTypes.Role, "STUDENT"),
            new Claim("hostel_block_id", "3"));

        var result = await _controller.GetMyNotices();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Single(Assert.IsAssignableFrom<IEnumerable<NoticeResponse>>(ok.Value));
        _repository.Verify(r => r.GetStudentNoticesAsync(It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public async Task GetStudentNotices_ForStaffPreview_ForwardsTheRequestedBlock()
    {
        _repository
            .Setup(r => r.GetStudentNoticesAsync(7UL))
            .ReturnsAsync(new List<NoticeResponse>());

        SignInAsStaff();

        var result = await _controller.GetStudentNotices(7UL);

        Assert.IsType<OkObjectResult>(result.Result);
        _repository.Verify(r => r.GetStudentNoticesAsync(7UL), Times.Once);
    }
}
