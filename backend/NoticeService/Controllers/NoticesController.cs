using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoticeService.DTOs;
using NoticeService.Repositories;

namespace NoticeService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NoticesController : ControllerBase
{
    private readonly INoticeRepository _noticeRepository;

    public NoticesController(INoticeRepository noticeRepository)
    {
        _noticeRepository = noticeRepository;
    }

    [HttpPost]
    [Authorize(Roles = "WARDEN,HOSTEL_MASTER,ADMIN")]
    public async Task<IActionResult> CreateNotice([FromBody] CreateNoticeRequest request)
    {
        var (userId, userRole) = GetActorDetails();
        if (!userId.HasValue || string.IsNullOrEmpty(userRole))
            return Unauthorized(new { message = "Invalid user credentials in token." });

        var notice = await _noticeRepository.CreateAsync(request, userId.Value, userRole);
        return CreatedAtAction(nameof(GetNoticeById), new { id = notice.NoticeId }, notice);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NoticeResponse>>> GetAllNotices([FromQuery] bool includeArchived = false)
    {
        var notices = await _noticeRepository.GetAllAsync(includeArchived);
        return Ok(notices);
    }

    [HttpGet("{id:ulong}")]
    public async Task<IActionResult> GetNoticeById(ulong id)
    {
        var notice = await _noticeRepository.GetByIdAsync(id);
        if (notice == null)
            return NotFound(new { message = $"Notice {id} not found." });

        return Ok(notice);
    }

    [HttpGet("student/{hostelBlockId:ulong}")]
    public async Task<IActionResult> GetStudentNotices(ulong hostelBlockId)
    {
        if (hostelBlockId == 0)
            return BadRequest(new { message = "Hostel block ID must be greater than zero." });

        var notices = await _noticeRepository.GetStudentNoticesAsync(hostelBlockId);
        return Ok(notices);
    }

    [HttpPut("{id:ulong}")]
    [Authorize(Roles = "WARDEN,HOSTEL_MASTER,ADMIN")]
    public async Task<IActionResult> UpdateNotice(ulong id, [FromBody] UpdateNoticeRequest request)
    {
        var (userId, userRole) = GetActorDetails();
        if (!userId.HasValue || string.IsNullOrEmpty(userRole))
            return Unauthorized(new { message = "Invalid user credentials in token." });

        var updated = await _noticeRepository.UpdateAsync(id, request, userId.Value, userRole);
        if (!updated)
            return NotFound(new { message = $"Notice {id} not found." });

        var updatedNotice = await _noticeRepository.GetByIdAsync(id);
        return Ok(updatedNotice);
    }

    [HttpDelete("{id:ulong}")]
    [Authorize(Roles = "WARDEN,HOSTEL_MASTER,ADMIN")]
    public async Task<IActionResult> DeleteNotice(ulong id)
    {
        var (userId, userRole) = GetActorDetails();
        if (!userId.HasValue || string.IsNullOrEmpty(userRole))
            return Unauthorized(new { message = "Invalid user credentials in token." });

        var deleted = await _noticeRepository.DeleteAsync(id, userId.Value, userRole);
        if (!deleted)
            return NotFound(new { message = $"Notice {id} not found." });

        return NoContent();
    }

    private (ulong? UserId, string? Role) GetActorDetails()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

        if (ulong.TryParse(userIdClaim, out var userId) && !string.IsNullOrEmpty(roleClaim))
            return (userId, roleClaim.ToUpper());

        return (null, null);
    }
}