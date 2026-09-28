using LeaveService.DTOs;
using LeaveService.Exceptions;
using LeaveService.Models;
using LeaveService.Tests.TestDoubles;

namespace LeaveService.Tests;

public class LeaveReportTests
{
    [Fact]
    public async Task GetReportAsync_CountsRequestsByStatusAndFlagsOverdue()
    {
        var repository = SeedMixedRequests();
        var service = CreateService(repository);

        var report = await service.GetReportAsync(new LeaveReportFilter());

        Assert.Equal(6, report.Totals.Total);
        Assert.Equal(1, report.Totals.Pending);
        Assert.Equal(1, report.Totals.Approved);
        Assert.Equal(1, report.Totals.Rejected);
        Assert.Equal(2, report.Totals.Departed);
        Assert.Equal(1, report.Totals.Closed);
        Assert.Equal(1, report.Totals.Overdue);
        Assert.Equal(6, report.Requests.Count);
    }

    [Fact]
    public async Task GetReportAsync_MarksOnlyDepartedStudentsPastTheirReturnDateAsOverdue()
    {
        var repository = SeedMixedRequests();
        var service = CreateService(repository);

        var report = await service.GetReportAsync(new LeaveReportFilter());

        var overdue = report.Requests.Where(request => request.IsOverdue).ToList();

        Assert.Equal("overdue-student", Assert.Single(overdue).StudentUsername);
        Assert.Equal(LeaveRequestStatuses.Departed, overdue[0].Status);
    }

    [Fact]
    public async Task GetReportAsync_StudentReturningToday_IsNotOverdueYet()
    {
        var repository = new FakeLeaveRequestRepository();
        repository.Seed(
            7,
            LeaveRequestStatuses.Departed,
            "due-today",
            expectedReturnDate: LeaveTestData.Today);

        var report = await CreateService(repository)
            .GetReportAsync(new LeaveReportFilter());

        Assert.False(Assert.Single(report.Requests).IsOverdue);
        Assert.Equal(0, report.Totals.Overdue);
    }

    [Fact]
    public async Task GetReportAsync_FiltersByStatus()
    {
        var service = CreateService(SeedMixedRequests());

        var report = await service.GetReportAsync(
            new LeaveReportFilter { Status = LeaveRequestStatuses.Rejected });

        var only = Assert.Single(report.Requests);

        Assert.Equal(LeaveRequestStatuses.Rejected, only.Status);
    }

    [Fact]
    public async Task GetReportAsync_StatusFilterIsNormalisedToUpperCase()
    {
        var repository = SeedMixedRequests();
        var service = CreateService(repository);

        await service.GetReportAsync(new LeaveReportFilter { Status = "  pending " });

        Assert.Equal(LeaveRequestStatuses.Pending, repository.LastFilter!.Status);
    }

    [Fact]
    public async Task GetReportAsync_FiltersByDepartureDateRange()
    {
        var repository = new FakeLeaveRequestRepository();
        repository.Seed(7, LeaveRequestStatuses.Pending, "early", departureDate: new DateOnly(2026, 9, 5));
        repository.Seed(8, LeaveRequestStatuses.Pending, "inside", departureDate: new DateOnly(2026, 9, 15));
        repository.Seed(9, LeaveRequestStatuses.Pending, "late", departureDate: new DateOnly(2026, 10, 20));

        var report = await CreateService(repository).GetReportAsync(
            new LeaveReportFilter
            {
                FromDate = new DateOnly(2026, 9, 10),
                ToDate = new DateOnly(2026, 9, 30)
            });

        Assert.Equal("inside", Assert.Single(report.Requests).StudentUsername);
        Assert.Equal(1, report.Totals.Total);
    }

    [Fact]
    public async Task GetReportAsync_DateRangeBoundariesAreInclusive()
    {
        var repository = new FakeLeaveRequestRepository();
        repository.Seed(7, LeaveRequestStatuses.Pending, "first-day", departureDate: new DateOnly(2026, 9, 10));
        repository.Seed(8, LeaveRequestStatuses.Pending, "last-day", departureDate: new DateOnly(2026, 9, 30));

        var report = await CreateService(repository).GetReportAsync(
            new LeaveReportFilter
            {
                FromDate = new DateOnly(2026, 9, 10),
                ToDate = new DateOnly(2026, 9, 30)
            });

        Assert.Equal(2, report.Requests.Count);
    }

    [Fact]
    public async Task GetReportAsync_OverdueOnly_ReturnsOnlyOverdueStudents()
    {
        var service = CreateService(SeedMixedRequests());

        var report = await service.GetReportAsync(
            new LeaveReportFilter { OverdueOnly = true });

        Assert.Equal("overdue-student", Assert.Single(report.Requests).StudentUsername);
    }

    [Fact]
    public async Task GetReportAsync_TotalsIgnoreTheStatusFilter()
    {
        var repository = SeedMixedRequests();
        var service = CreateService(repository);

        var report = await service.GetReportAsync(
            new LeaveReportFilter { Status = LeaveRequestStatuses.Pending });

        Assert.Single(report.Requests);
        Assert.Equal(6, report.Totals.Total);
        Assert.Equal(2, report.Totals.Departed);
    }

    [Fact]
    public async Task GetReportAsync_TotalsUseTheSameDateRangeAsTheList()
    {
        var repository = SeedMixedRequests();
        var service = CreateService(repository);

        await service.GetReportAsync(
            new LeaveReportFilter
            {
                FromDate = new DateOnly(2026, 9, 1),
                ToDate = new DateOnly(2026, 9, 30)
            });

        Assert.Equal(
            (new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30)),
            repository.LastTotalsRange!.Value);
    }

    [Theory]
    [InlineData("OVERDUE")]
    [InlineData("nonsense")]
    public async Task GetReportAsync_WithUnknownStatus_IsRejected(string status)
    {
        var service = CreateService(new FakeLeaveRequestRepository());

        var exception = await Assert.ThrowsAsync<LeaveRequestValidationException>(
            () => service.GetReportAsync(new LeaveReportFilter { Status = status }));

        Assert.True(exception.Errors.ContainsKey("status"));
    }

    [Fact]
    public async Task GetReportAsync_WithEndDateBeforeStartDate_IsRejected()
    {
        var service = CreateService(new FakeLeaveRequestRepository());

        var exception = await Assert.ThrowsAsync<LeaveRequestValidationException>(
            () => service.GetReportAsync(
                new LeaveReportFilter
                {
                    FromDate = new DateOnly(2026, 9, 30),
                    ToDate = new DateOnly(2026, 9, 1)
                }));

        Assert.True(exception.Errors.ContainsKey("toDate"));
    }

    [Fact]
    public async Task GetReportAsync_StampsTheTimeItWasGenerated()
    {
        var report = await CreateService(new FakeLeaveRequestRepository())
            .GetReportAsync(new LeaveReportFilter());

        Assert.Equal(LeaveTestData.Now, report.GeneratedAtUtc);
    }

    [Fact]
    public async Task StudentView_ShowsDecisionReasonAndOverdueFlag()
    {
        var repository = new FakeLeaveRequestRepository();
        repository.Seed(7, LeaveRequestStatuses.Rejected, "student7");
        repository.Seed(
            7,
            LeaveRequestStatuses.Departed,
            "student7",
            expectedReturnDate: new DateOnly(2026, 9, 1));

        var service = LeaveTestData.CreateRequestService(
            repository,
            new FakeLeaveEventPublisher());

        var result = await service.GetMyRequestsAsync(7);

        Assert.Contains(result, request =>
            request.Status == LeaveRequestStatuses.Rejected &&
            request.DecisionReason == "Seeded decision");

        Assert.Contains(result, request => request.IsOverdue);
    }

    // 6 requests: one in each status, plus a second departed
    // student who is not yet overdue.
    private static FakeLeaveRequestRepository SeedMixedRequests()
    {
        var repository = new FakeLeaveRequestRepository();

        DateOnly departure = new(2026, 9, 10);

        repository.Seed(1, LeaveRequestStatuses.Pending, "pending-student", departure);
        repository.Seed(2, LeaveRequestStatuses.Approved, "approved-student", departure);
        repository.Seed(3, LeaveRequestStatuses.Rejected, "rejected-student", departure);
        repository.Seed(
            4,
            LeaveRequestStatuses.Departed,
            "overdue-student",
            departure,
            expectedReturnDate: LeaveTestData.Today.AddDays(-2));
        repository.Seed(
            5,
            LeaveRequestStatuses.Departed,
            "away-student",
            departure,
            expectedReturnDate: LeaveTestData.Today.AddDays(3));
        repository.Seed(6, LeaveRequestStatuses.Closed, "closed-student", departure);

        return repository;
    }

    private static Services.LeaveReviewService CreateService(
        FakeLeaveRequestRepository repository)
    {
        return LeaveTestData.CreateReviewService(
            repository,
            new FakeLeaveEventPublisher());
    }
}
