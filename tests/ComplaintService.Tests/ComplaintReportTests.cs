using System.Text;
using ComplaintService.Exceptions;
using ComplaintService.Models;
using ComplaintService.Services;
using ComplaintService.Tests.TestDoubles;
using NSubstitute;
using Xunit;

namespace ComplaintService.Tests;

public class ComplaintReportTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 6, 10, 0, 0, TimeSpan.Zero);

    private static StaffComplaintService CreateService(
        FakeComplaintRepository repository) =>
        new(repository, new FixedTimeProvider(Now),
            Substitute.For<INotificationPublisher>());

    private static FakeComplaintRepository SeedMixed()
    {
        var repo = new FakeComplaintRepository();
        repo.Seed(1, ComplaintStatuses.Open, "PLUMBING");
        repo.Seed(2, ComplaintStatuses.InProgress, "PLUMBING");
        repo.Seed(3, ComplaintStatuses.Resolved, "NOISE");
        repo.Seed(4, ComplaintStatuses.Open, "NOISE");
        return repo;
    }

    [Fact]
    public async Task Report_CountsByStatusAndCategory()
    {
        var report = await CreateService(SeedMixed()).GetReportAsync(null, null);

        Assert.Equal(4, report.Totals.Total);
        Assert.Equal(2, report.Totals.Open);
        Assert.Equal(1, report.Totals.InProgress);
        Assert.Equal(1, report.Totals.Resolved);
        Assert.Equal(4, report.Complaints.Count);
        Assert.Equal(2, report.ByCategory.Count);
        Assert.Equal(Now, report.GeneratedAtUtc);
    }

    [Fact]
    public async Task Report_StatusFilter_NarrowsListButNotTotals()
    {
        var report = await CreateService(SeedMixed()).GetReportAsync("open", null);

        Assert.Equal(2, report.Complaints.Count);
        Assert.All(report.Complaints, c => Assert.Equal("OPEN", c.Status));
        Assert.Equal(4, report.Totals.Total);
    }

    [Fact]
    public async Task Report_CategoryAndStatusFilter_Combine()
    {
        var report = await CreateService(SeedMixed()).GetReportAsync("OPEN", "noise");

        Assert.Single(report.Complaints);
        Assert.Equal(2, report.Totals.Total);
    }

    [Theory]
    [InlineData("nonsense", null)]
    [InlineData(null, "nonsense")]
    public async Task Report_InvalidFilter_IsRejected(string? status, string? category)
    {
        await Assert.ThrowsAsync<ComplaintValidationException>(
            () => CreateService(new FakeComplaintRepository())
                .GetReportAsync(status, category));
    }

    [Fact]
    public async Task Csv_HasHeaderAndRows()
    {
        var bytes = await CreateService(SeedMixed()).GenerateReportCsvAsync(null, null);
        string csv = Encoding.UTF8.GetString(bytes);

        Assert.Contains("Complaint ID", csv);
        Assert.Contains("PLUMBING", csv);
        Assert.Contains("RESOLVED", csv);
    }

    [Fact]
    public async Task Csv_NeutralisesFormulaInjection()
    {
        var repo = new FakeComplaintRepository();
        var c = repo.Seed(1);
        c.Description = "=HYPERLINK(\"http://evil\")";

        var bytes = await CreateService(repo).GenerateReportCsvAsync(null, null);
        string csv = Encoding.UTF8.GetString(bytes);

        Assert.Contains("'=HYPERLINK", csv);
    }
}