namespace NoticeService.DTOs;

public record CreateNoticeRequest(
    string Title,
    string Content,
    string NoticeType,
    ulong? HostelBlockId,
    DateOnly ExpiryDate
);

public record UpdateNoticeRequest(
    string Title,
    string Content,
    string NoticeType,
    ulong? HostelBlockId,
    DateOnly ExpiryDate
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