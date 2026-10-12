using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NoticeService.Controllers;
using NoticeService.DTOs;
using NoticeService.Repositories;
using NoticeService.Services;
using Xunit;

namespace NoticeService.Tests;

public class NoticesControllerTests
{
    private readonly Mock<INoticeRepository> _mockRepository;
    private readonly Mock<IBlockDirectory> _mockBlockDirectory;
    private readonly NoticesController _controller;

    public NoticesControllerTests()
    {
        _mockRepository = new Mock<INoticeRepository>();
        _mockBlockDirectory = new Mock<IBlockDirectory>();
        _mockBlockDirectory
            .Setup(b => b.IsActiveBlockAsync(It.IsAny<ulong>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _controller = new NoticesController(_mockRepository.Object, _mockBlockDirectory.Object);

        SetUserContext(10, "WARDEN");
    }

    private void SetUserContext(ulong userId, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    private void SetStudentContext(ulong userId, string? hostelBlockId)
    {
        var claims = new List<Claim>
        {
            new("sub", userId.ToString()),
            new(ClaimTypes.Role, "STUDENT")
        };

        if (hostelBlockId is not null)
        {
            claims.Add(new Claim("hostel_block_id", hostelBlockId));
        }

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            }
        };
    }

    [Fact]
    public async Task CreateNotice_ReturnsCreatedAtAction_WhenRequestIsValid()
    {
        var request = new CreateNoticeRequest("Hostel Meeting", "Mandatory attendance.", "NOTICE", 1, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)));
        var response = new NoticeResponse(1, request.Title, request.Content, request.NoticeType, request.HostelBlockId, request.ExpiryDate, false, null, 10, DateTime.UtcNow, DateTime.UtcNow);

        _mockRepository.Setup(r => r.CreateAsync(request, 10, "WARDEN")).ReturnsAsync(response);

        var result = await _controller.CreateNotice(request);

        var createdAtResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, createdAtResult.StatusCode);
    }

    [Fact]
    public async Task CreateNotice_ReadsTheUserIdFromTheSubClaim()
    {
        var request = new CreateNoticeRequest("Title", "Body", "NOTICE", null,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)));
        var response = new NoticeResponse(1, "Title", "Body", "NOTICE", null,
            request.ExpiryDate, false, null, 10, DateTime.UtcNow, DateTime.UtcNow);

        _mockRepository.Setup(r => r.CreateAsync(request, 10UL, "WARDEN")).ReturnsAsync(response);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim("sub", "10"), new Claim(ClaimTypes.Role, "WARDEN") },
                    "TestAuth"))
            }
        };

        var result = await _controller.CreateNotice(request);
        Assert.IsType<CreatedAtActionResult>(result);
    }

    [Fact]
    public async Task GetNoticeById_ReturnsNotFound_WhenNoticeDoesNotExist()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(99UL)).ReturnsAsync((NoticeResponse?)null);

        var result = await _controller.GetNoticeById(99UL);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateNotice_ReturnsOk_WhenNoticeExists()
    {
        var request = new UpdateNoticeRequest("Updated Meeting", "Updated body text", "SCHEDULE", 1, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)));
        var response = new NoticeResponse(1, request.Title, request.Content, request.NoticeType, request.HostelBlockId, request.ExpiryDate, false, null, 10, DateTime.UtcNow, DateTime.UtcNow);

        _mockRepository.Setup(r => r.UpdateAsync(1UL, request, 10, "WARDEN")).ReturnsAsync(true);
        _mockRepository.Setup(r => r.GetByIdAsync(1UL)).ReturnsAsync(response);

        var result = await _controller.UpdateNotice(1UL, request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task DeleteNotice_ReturnsNoContent_WhenDeleteIsSuccessful()
    {
        _mockRepository.Setup(r => r.DeleteAsync(1UL, 10, "WARDEN")).ReturnsAsync(true);

        var result = await _controller.DeleteNotice(1UL);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task GetMyNotices_UsesTheBlockFromTheToken()
    {
        _mockRepository
            .Setup(r => r.GetStudentNoticesAsync(2UL))
            .ReturnsAsync(new List<NoticeResponse>());

        SetStudentContext(201, "2");

        var actionResult = await _controller.GetMyNotices();

        Assert.IsType<OkObjectResult>(actionResult.Result);
        _mockRepository.Verify(r => r.GetStudentNoticesAsync(2UL), Times.Once);
    }

    [Fact]
    public async Task GetMyNotices_WithoutABlockClaim_ReturnsOnlyGeneralNotices()
    {
        _mockRepository
            .Setup(r => r.GetStudentNoticesAsync(0UL))
            .ReturnsAsync(new List<NoticeResponse>());

        SetStudentContext(201, null);

        var actionResult = await _controller.GetMyNotices();

        Assert.IsType<OkObjectResult>(actionResult.Result);
        _mockRepository.Verify(r => r.GetStudentNoticesAsync(0UL), Times.Once);
    }

    [Fact]
    public async Task GetStudentNotices_ReturnsOk_WithBlockAndGeneralNotices()
    {
        ulong studentBlockId = 2UL;
        var notices = new List<NoticeResponse>
        {
            new NoticeResponse(10, "Block 2 Water Maintenance", "Water off at 2 PM.", "NOTICE", 2UL, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), false, null, 101, DateTime.UtcNow, DateTime.UtcNow),
            new NoticeResponse(11, "General Notice", "Hostel Rules Update.", "NOTICE", null, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), false, null, 101, DateTime.UtcNow, DateTime.UtcNow)
        };

        _mockRepository
            .Setup(r => r.GetStudentNoticesAsync(studentBlockId))
            .ReturnsAsync(notices);

        SetUserContext(201, "STUDENT");

        var actionResult = await _controller.GetStudentNotices(studentBlockId);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Equal(200, okResult.StatusCode);

        var returnedNotices = Assert.IsAssignableFrom<IEnumerable<NoticeResponse>>(okResult.Value);
        Assert.Equal(2, returnedNotices.Count());
    }

    [Fact]
    public async Task GetStudentNotices_ReturnsBadRequest_WhenBlockIdIsZero()
    {
        var actionResult = await _controller.GetStudentNotices(0);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task GetStudentNotices_ReturnsEmptyList_WhenNoActiveNoticesExist()
    {
        ulong studentBlockId = 99UL;
        _mockRepository
            .Setup(r => r.GetStudentNoticesAsync(studentBlockId))
            .ReturnsAsync(new List<NoticeResponse>());

        SetUserContext(202, "STUDENT");

        var actionResult = await _controller.GetStudentNotices(studentBlockId);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returnedNotices = Assert.IsAssignableFrom<IEnumerable<NoticeResponse>>(okResult.Value);
        Assert.Empty(returnedNotices);
    }
}