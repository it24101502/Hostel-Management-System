namespace NoticeService.Repositories;

using NoticeService.DTOs;

public interface INoticeRepository
{
    Task<NoticeResponse> CreateAsync(CreateNoticeRequest request, ulong userId, string userRole);
    Task<NoticeResponse?> GetByIdAsync(ulong noticeId);
    Task<IEnumerable<NoticeResponse>> GetAllAsync(bool includeArchived = false);
    Task<IEnumerable<NoticeResponse>> GetStudentNoticesAsync(ulong hostelBlockId);
    Task<bool> UpdateAsync(ulong noticeId, UpdateNoticeRequest request, ulong userId, string userRole);
    Task<bool> DeleteAsync(ulong noticeId, ulong userId, string userRole);
}