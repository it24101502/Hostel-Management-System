using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoticeService.Authorization;
using NoticeService.DTOs;
using NoticeService.Models;
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

    private bool TryGetUserInfo(out ulong userId, out string userRole)
    {
        userId = 0;
        userRole = string.Empty;

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value
                     ?? User.FindFirst("role")?.Value;

        if (!ulong.TryParse(userIdClaim, out userId) || string.IsNullOrWhiteSpace(roleClaim))
        {
            return false;
        }

        userRole = roleClaim.ToUpper();
        return true;
    }

    [HttpPost]
    [RequireRole(NoticeRoles.Warden, NoticeRoles.HostelMaster, NoticeRoles.Admin)]
    public async Task<IActionResult> CreateNotice([FromBody] CreateNoticeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new ErrorResponse { Message = "Title and Content cannot be empty." });
        }

        if (!NoticeTypes.All.Contains(request.NoticeType))
        {
            return BadRequest(new ErrorResponse { Message = "Invalid notice type. Allowed values are NOTICE and SCHEDULE." });
        }

        if (!TryGetUserInfo(out var userId, out var userRole))
        {
            return Unauthorized(new ErrorResponse { Message = "Invalid user claims." });
        }

        var newNoticeId = await _noticeRepository.CreateAsync(request, userId, userRole);
        var createdNotice = await _noticeRepository.GetByIdAsync(newNoticeId);

        return CreatedAtAction(nameof(GetNoticeById), new { id = newNoticeId }, createdNotice);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllNotices([FromQuery] bool includeArchived = false)
    {
        var notices = await _noticeRepository.GetAllAsync(includeArchived);
        return Ok(notices);
    }

    [HttpGet("{id:ulong}")]
    public async Task<IActionResult> GetNoticeById(ulong id)
    {
        var notice = await _noticeRepository.GetByIdAsync(id);
        if (notice == null)
        {
            return NotFound(new ErrorResponse { Message = $"Notice with ID {id} was not found." });
        }

        return Ok(notice);
    }

    [HttpPut("{id:ulong}")]
    [RequireRole(NoticeRoles.Warden, NoticeRoles.HostelMaster, NoticeRoles.Admin)]
    public async Task<IActionResult> UpdateNotice(ulong id, [FromBody] UpdateNoticeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new ErrorResponse { Message = "Title and Content cannot be empty." });
        }

        if (!NoticeTypes.All.Contains(request.NoticeType))
        {
            return BadRequest(new ErrorResponse { Message = "Invalid notice type. Allowed values are NOTICE and SCHEDULE." });
        }

        if (!TryGetUserInfo(out var userId, out var userRole))
        {
            return Unauthorized(new ErrorResponse { Message = "Invalid user claims." });
        }

        var updated = await _noticeRepository.UpdateAsync(id, request, userId, userRole);
        if (!updated)
        {
            return NotFound(new ErrorResponse { Message = $"Notice with ID {id} was not found." });
        }

        var updatedNotice = await _noticeRepository.GetByIdAsync(id);
        return Ok(updatedNotice);
    }

    [HttpDelete("{id:ulong}")]
    [RequireRole(NoticeRoles.Warden, NoticeRoles.HostelMaster, NoticeRoles.Admin)]
    public async Task<IActionResult> DeleteNotice(ulong id)
    {
        if (!TryGetUserInfo(out var userId, out var userRole))
        {
            return Unauthorized(new ErrorResponse { Message = "Invalid user claims." });
        }

        var deleted = await _noticeRepository.DeleteAsync(id, userId, userRole);
        if (!deleted)
        {
            return NotFound(new ErrorResponse { Message = $"Notice with ID {id} was not found." });
        }

        return NoContent();
    }
}