using Microsoft.Extensions.Logging;
using Moq;
using NoticeService.Jobs;
using NoticeService.Repositories;
using NoticeService.Services;
using Quartz;
using Xunit;
namespace NoticeService.Tests;

public class NoticeArchivalJobTests
{
    private readonly Mock<INoticeRepository> _mockRepository;
    private readonly Mock<IDateTimeProvider> _mockTimeProvider;
    private readonly Mock<ILogger<NoticeArchivalJob>> _mockLogger;
    private readonly Mock<IJobExecutionContext> _mockJobContext;
    private readonly NoticeArchivalJob _job;

    public NoticeArchivalJobTests()
    {
        _mockRepository = new Mock<INoticeRepository>();
        _mockTimeProvider = new Mock<IDateTimeProvider>();
        _mockLogger = new Mock<ILogger<NoticeArchivalJob>>();
        _mockJobContext = new Mock<IJobExecutionContext>();

        _job = new NoticeArchivalJob(_mockRepository.Object, _mockTimeProvider.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Execute_CallsArchiveExpiredNoticesAsync_WithSimulatedDate()
    {
        // Arrange: Simulate current date as October 10, 2026
        var simulatedToday = new DateOnly(2026, 10, 10);
        _mockTimeProvider.Setup(t => t.Today).Returns(simulatedToday);
        _mockTimeProvider.Setup(t => t.UtcNow).Returns(new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc));

        _mockRepository
            .Setup(r => r.ArchiveExpiredNoticesAsync(simulatedToday))
            .ReturnsAsync(5);

        // Act
        await _job.Execute(_mockJobContext.Object, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.ArchiveExpiredNoticesAsync(simulatedToday), Times.Once);
    }

    [Fact]
    public async Task Execute_LogsArchivedCount_WhenArchivalSucceeds()
    {
        // Arrange
        var today = new DateOnly(2026, 10, 12);
        _mockTimeProvider.Setup(t => t.Today).Returns(today);
        _mockRepository
            .Setup(r => r.ArchiveExpiredNoticesAsync(today))
            .ReturnsAsync(3);

        // Act
        await _job.Execute(_mockJobContext.Object, CancellationToken.None);

        // Assert
        VerifyLogged(LogLevel.Information, "Total notices archived: 3", Times.Once());
        VerifyLogged(LogLevel.Error, "", Times.Never());
    }

    [Fact]
    public async Task Execute_ThrowsJobExecutionException_WhenRepositoryFails()
    {
        // Arrange
        var simulatedToday = new DateOnly(2026, 10, 15);
        _mockTimeProvider.Setup(t => t.Today).Returns(simulatedToday);
        _mockRepository
            .Setup(r => r.ArchiveExpiredNoticesAsync(simulatedToday))
            .ThrowsAsync(new Exception("Database connection failure"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<JobExecutionException>(async () => 
            await _job.Execute(_mockJobContext.Object, CancellationToken.None));
            
        Assert.NotNull(exception.InnerException);
        Assert.Equal("Database connection failure", exception.InnerException.Message);
        
        VerifyLogged(LogLevel.Error, "An error occurred while executing", Times.Once());
    }

    private void VerifyLogged(LogLevel level, string containing, Times times) =>
        _mockLogger.Verify(
            l => l.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains(containing)),
                It.IsAny<Exception?>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            times);
}