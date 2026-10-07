using NoticeService.Repositories;
using NoticeService.Services;
using Quartz;

namespace NoticeService.Jobs;

[DisallowConcurrentExecution]
public class NoticeArchivalJob : IJob
{
    private readonly INoticeRepository _noticeRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<NoticeArchivalJob> _logger;

    public NoticeArchivalJob(
        INoticeRepository noticeRepository, 
        IDateTimeProvider dateTimeProvider, 
        ILogger<NoticeArchivalJob> logger)
    {
        _noticeRepository = noticeRepository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Notice Archival Job started at {Time}", _dateTimeProvider.UtcNow);

        try
        {
            var simulatedOrCurrentDate = _dateTimeProvider.Today;
            var archivedCount = await _noticeRepository.ArchiveExpiredNoticesAsync(simulatedOrCurrentDate);

            _logger.LogInformation("Notice Archival Job completed successfully. Total notices archived: {Count}", archivedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while executing the Notice Archival Job.");
            throw new JobExecutionException(ex) { RefireImmediately = false };
        }
    }
}