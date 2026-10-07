using System.ComponentModel.DataAnnotations;

namespace NoticeService.DTOs;

public record CreateNoticeRequest(
    [Required, StringLength(200, MinimumLength = 1)] string Title,
    [Required, StringLength(4000, MinimumLength = 1)] string Content,
    [Required, RegularExpression("^(NOTICE|SCHEDULE)$", ErrorMessage = "NoticeType must be 'NOTICE' or 'SCHEDULE'")] string NoticeType,
    ulong? HostelBlockId,
    [Required] DateOnly ExpiryDate
);

public record UpdateNoticeRequest(
    [Required, StringLength(200, MinimumLength = 1)] string Title,
    [Required, StringLength(4000, MinimumLength = 1)] string Content,
    [Required, RegularExpression("^(NOTICE|SCHEDULE)$", ErrorMessage = "NoticeType must be 'NOTICE' or 'SCHEDULE'")] string NoticeType,
    ulong? HostelBlockId,
    [Required] DateOnly ExpiryDate
);

public record NoticeResponse(
    ulong NoticeId,
    string Title,
    string Content,
    string NoticeType,
    ulong? HostelBlockId,
    DateOnly ExpiryDate,
    bool IsArchived,
    DateTime? ArchivedAt,
    ulong CreatedByUserId,
    DateTime CreatedAt,
    DateTime UpdatedAt
);