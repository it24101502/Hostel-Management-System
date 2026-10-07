using System.IdentityModel.Tokens.Jwt;
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

    // Staff only: this list can include other blocks and archived items.
    [HttpGet]
    [Authorize(Roles = "WARDEN,HOSTEL_MASTER,ADMIN")]
    public async Task<ActionResult<IEnumerable<NoticeResponse>>> GetAllNotices(
        [FromQuery] bool includeArchived = false)
    {
        var notices = await _noticeRepository.GetAllAsync(includeArchived);
        return Ok(notices);
    }

    // Staff only: students use the block-filtered endpoint below.
    [HttpGet("{id:long}")]
    [Authorize(Roles = "WARDEN,HOSTEL_MASTER,ADMIN")]
    public async Task<IActionResult> GetNoticeById(ulong id)
    {
        var notice = await _noticeRepository.GetByIdAsync(id);
        if (notice == null)
            return NotFound(new { message = $"Notice {id} not found." });

        return Ok(notice);
    }

    /// <summary>
    /// The signed-in student's own notices: their block's plus general ones.
    /// The block comes from the JWT claim, so a student cannot request another block.
    /// A student with no block assigned sees only general notices (hostel_block_id IS NULL).
    /// </summary>
    [HttpGet("my")]
    [Authorize(Roles = "STUDENT")]
    public async Task<ActionResult<IEnumerable<NoticeResponse>>> GetMyNotices()
    {
        // Block 0 matches no specific block ID, so the query returns only
        // general notices (hostel_block_id IS NULL).
        ulong blockId = 0;
        string? claimValue = User.FindFirst("hostel_block_id")?.Value;
        if (claimValue is not null && ulong.TryParse(claimValue, out ulong parsed))
        {
            blockId = parsed;
        }

        var notices = await _noticeRepository.GetStudentNoticesAsync(blockId);
        return Ok(notices);
    }

    /// <summary>
    /// Staff preview of what students in a given block see. (HMS-61)
    /// </summary>
    [HttpGet("student/{hostelBlockId:long}")]
    [Authorize(Roles = "WARDEN,HOSTEL_MASTER,ADMIN")]
    public async Task<ActionResult<IEnumerable<NoticeResponse>>> GetStudentNotices(ulong hostelBlockId)
    {
        if (hostelBlockId == 0)
        {
            return BadRequest(new { message = "Hostel block ID must be greater than zero." });
        }

        var notices = await _noticeRepository.GetStudentNoticesAsync(hostelBlockId);
        return Ok(notices);
    }

    [HttpPut("{id:long}")]
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

    [HttpDelete("{id:long}")]
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
        // IdentityService puts the user ID in the "sub" claim. With
        // MapInboundClaims = false it is not renamed to NameIdentifier.
        string? userIdClaim =
            User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        string? roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

        if (ulong.TryParse(userIdClaim, out var userId) && !string.IsNullOrEmpty(roleClaim))
            return (userId, roleClaim.ToUpperInvariant());

        return (null, null);
    }
}