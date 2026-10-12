using Microsoft.Extensions.Logging;
using Moq;
using NoticeService.Jobs;
using NoticeService.Repositories;
using NoticeService.Services;
using Quartz;
using Xunit;

namespace NoticeService.Tests;

/// <summary>
/// HMS-62 cases not covered by NoticeArchivalJobTests: nothing to archive,
/// a single repository call per run, the no-retry rule and the date the
/// job passes on.
/// </summary>
public class NoticeArchivalJobAdditionalTests
{
    private readonly Mock<INoticeRepository> _repository = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<ILogger<NoticeArchivalJob>> _logger = new();
    private readonly Mock<IJobExecutionContext> _context = new();
    private readonly NoticeArchivalJob _job;

    public NoticeArchivalJobAdditionalTests()
    {
        _job = new NoticeArchivalJob(
            _repository.Object,
            _clock.Object,
            _logger.Object);
    }

    [Fact]
    public async Task Execute_WhenNothingHasExpired_CompletesWithoutErrors()
    {
        var today = new DateOnly(2026, 10, 10);
        _clock.Setup(c => c.Today).Returns(today);
        _repository.Setup(r => r.ArchiveExpiredNoticesAsync(today)).ReturnsAsync(0);

        await _job.Execute(_context.Object, CancellationToken.None);

        VerifyLogged(LogLevel.Information, "Total notices archived: 0", Times.Once());
        VerifyLogged(LogLevel.Error, "", Times.Never());
    }

    [Fact]
    public async Task Execute_CallsTheRepositoryExactlyOncePerRun()
    {
        var today = new DateOnly(2026, 10, 10);
        _clock.Setup(c => c.Today).Returns(today);
        _repository.Setup(r => r.ArchiveExpiredNoticesAsync(today)).ReturnsAsync(2);

        await _job.Execute(_context.Object, CancellationToken.None);

        _repository.Verify(
            r => r.ArchiveExpiredNoticesAsync(It.IsAny<DateOnly>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_PassesTheClockDate_NotTheRealDate()
    {
        var simulated = new DateOnly(2031, 3, 4);
        _clock.Setup(c => c.Today).Returns(simulated);
        _repository
            .Setup(r => r.ArchiveExpiredNoticesAsync(simulated))
            .ReturnsAsync(1);

        await _job.Execute(_context.Object, CancellationToken.None);

        _repository.Verify(r => r.ArchiveExpiredNoticesAsync(simulated), Times.Once);
        _repository.Verify(
            r => r.ArchiveExpiredNoticesAsync(
                DateOnly.FromDateTime(DateTime.UtcNow)),
            Times.Never);
    }

    [Fact]
    public async Task Execute_WhenTheRepositoryFails_AsksQuartzNotToRefireImmediately()
    {
        var today = new DateOnly(2026, 10, 10);
        _clock.Setup(c => c.Today).Returns(today);
        _repository
            .Setup(r => r.ArchiveExpiredNoticesAsync(today))
            .ThrowsAsync(new InvalidOperationException("Database is down."));

        var exception = await Assert.ThrowsAsync<JobExecutionException>(
            async () => await _job.Execute(_context.Object, CancellationToken.None));

        Assert.False(exception.RefireImmediately);
    }

    [Fact]
    public async Task Execute_WhenTheRepositoryFails_DoesNotLogASuccessMessage()
    {
        var today = new DateOnly(2026, 10, 10);
        _clock.Setup(c => c.Today).Returns(today);
        _repository
            .Setup(r => r.ArchiveExpiredNoticesAsync(today))
            .ThrowsAsync(new InvalidOperationException("Database is down."));

        await Assert.ThrowsAsync<JobExecutionException>(
            async () => await _job.Execute(_context.Object, CancellationToken.None));

        VerifyLogged(LogLevel.Information, "completed successfully", Times.Never());
    }

    [Fact]
    public async Task Execute_LogsWhenTheRunStarts()
    {
        var today = new DateOnly(2026, 10, 10);
        _clock.Setup(c => c.Today).Returns(today);
        _clock.Setup(c => c.UtcNow).Returns(
            new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc));
        _repository.Setup(r => r.ArchiveExpiredNoticesAsync(today)).ReturnsAsync(0);

        await _job.Execute(_context.Object, CancellationToken.None);

        VerifyLogged(LogLevel.Information, "started", Times.Once());
    }

    [Fact]
    public async Task Execute_CanRunTwiceInARow_AndAsksTheRepositoryEachTime()
    {
        var today = new DateOnly(2026, 10, 10);
        _clock.Setup(c => c.Today).Returns(today);
        _repository
            .SetupSequence(r => r.ArchiveExpiredNoticesAsync(today))
            .ReturnsAsync(3)
            .ReturnsAsync(0);

        await _job.Execute(_context.Object, CancellationToken.None);
        await _job.Execute(_context.Object, CancellationToken.None);

        _repository.Verify(r => r.ArchiveExpiredNoticesAsync(today), Times.Exactly(2));
        VerifyLogged(LogLevel.Information, "Total notices archived: 3", Times.Once());
        VerifyLogged(LogLevel.Information, "Total notices archived: 0", Times.Once());
    }

    private void VerifyLogged(LogLevel level, string containing, Times times) =>
        _logger.Verify(
            l => l.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains(containing)),
                It.IsAny<Exception?>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            times);
}
