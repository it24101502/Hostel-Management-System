using NoticeService.DTOs;

namespace NoticeService.Repositories;

public interface INoticeRepository
{
    Task<ulong> CreateAsync(CreateNoticeRequest request, ulong userId, string userRole);
    Task<NoticeResponse?> GetByIdAsync(ulong noticeId);
    Task<IEnumerable<NoticeResponse>> GetAllAsync(bool includeArchived = false);
    Task<bool> UpdateAsync(ulong noticeId, UpdateNoticeRequest request, ulong userId, string userRole);
    Task<bool> DeleteAsync(ulong noticeId, ulong userId, string userRole);
}