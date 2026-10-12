using System.Text;
using ComplaintService.DTOs;
using ComplaintService.Exceptions;
using ComplaintService.Models;
using ComplaintService.Repositories;
using ComplaintService.Services;
using ComplaintService.Tests.TestDoubles;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ComplaintService.Tests;

public class StaffComplaintServiceAdditionalTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 6, 10, 0, 0, TimeSpan.Zero);

    private static StaffComplaintService CreateService(
        IComplaintRepository repository,
        INotificationPublisher? publisher = null,
        IStaffDirectory? staffDirectory = null)
    {
        var staffDir = staffDirectory ?? Substitute.For<IStaffDirectory>();
        staffDir.IsActiveStaffAsync(Arg.Any<ulong>(), Arg.Any<CancellationToken>()).Returns(true);
        return new(repository,
            new FixedTimeProvider(Now),
            publisher ?? Substitute.For<INotificationPublisher>(),
            staffDir);
    }

    private static (IComplaintRepository Repository, Complaint Complaint)
        CreateMockedRepository(string status = ComplaintStatuses.Open)
    {
        var repository = Substitute.For<IComplaintRepository>();

        var complaint = new Complaint
        {
            ComplaintId = 101,
            StudentUserId = 500,
            StudentUsername = "student500",
            Category = "PLUMBING",
            Description = "Leaking tap",
            Status = status
        };

        repository.GetByIdAsync(101UL).Returns(complaint);

        repository.TryChangeStatusAsync(
                Arg.Any<ulong>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ulong>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<DateTime>())
            .Returns(true);

        return (repository, complaint);
    }

    [Fact]
    public async Task ChangeStatus_SavesANotificationForTheOwningStudent()
    {
        var (repository, _) = CreateMockedRepository();

        await CreateService(repository).ChangeStatusAsync(
            101,
            new UpdateComplaintStatusRequest { Status = "RESOLVED" },
            20,
            ComplaintRoles.Warden);

        await repository.Received(1).AddNotificationAsync(
            Arg.Any<Guid>(),
            101UL,
            500UL,
            Arg.Is<string>(message =>
                message.Contains("#101") &&
                message.Contains("resolved") &&
                message.Contains("was open")),
            Now.UtcDateTime);
    }

    [Fact]
    public async Task ChangeStatus_DescribesInProgressInPlainWords()
    {
        var (repository, _) = CreateMockedRepository();

        await CreateService(repository).ChangeStatusAsync(
            101,
            new UpdateComplaintStatusRequest { Status = "in_progress" },
            20,
            ComplaintRoles.Warden);

        await repository.Received(1).AddNotificationAsync(
            Arg.Any<Guid>(),
            Arg.Any<ulong>(),
            Arg.Any<ulong>(),
            Arg.Is<string>(message => message.Contains("in progress")),
            Arg.Any<DateTime>());
    }

    [Fact]
    public async Task ChangeStatus_WhenAnotherUserChangedItFirst_SavesNoNotificationAndNoEvent()
    {
        var (repository, _) = CreateMockedRepository();

        repository.TryChangeStatusAsync(
                Arg.Any<ulong>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ulong>(),
                Arg.Any<string>(),
                Arg.Any<string?>(),
                Arg.Any<DateTime>())
            .Returns(false);

        var publisher = Substitute.For<INotificationPublisher>();

        await Assert.ThrowsAsync<InvalidComplaintStatusException>(
            () => CreateService(repository, publisher).ChangeStatusAsync(
                101,
                new UpdateComplaintStatusRequest { Status = "RESOLVED" },
                20,
                ComplaintRoles.Warden));

        await repository.DidNotReceive().AddNotificationAsync(
            Arg.Any<Guid>(),
            Arg.Any<ulong>(),
            Arg.Any<ulong>(),
            Arg.Any<string>(),
            Arg.Any<DateTime>());

        await publisher.DidNotReceive().PublishStatusChangeNotificationAsync(
            Arg.Any<ComplaintStatusChangedEvent>());
    }

    [Fact]
    public async Task ChangeStatus_WithInvalidStatus_SavesNoNotification()
    {
        var (repository, _) = CreateMockedRepository();

        await Assert.ThrowsAsync<ComplaintValidationException>(
            () => CreateService(repository).ChangeStatusAsync(
                101,
                new UpdateComplaintStatusRequest { Status = "DONE" },
                20,
                ComplaintRoles.Warden));

        await repository.DidNotReceive().AddNotificationAsync(
            Arg.Any<Guid>(),
            Arg.Any<ulong>(),
            Arg.Any<ulong>(),
            Arg.Any<string>(),
            Arg.Any<DateTime>());
    }

    [Fact]
    public async Task ChangeStatus_PublishesAnEventWithOldAndNewStatusAndStudent()
    {
        var (repository, _) = CreateMockedRepository();
        var publisher = Substitute.For<INotificationPublisher>();

        await CreateService(repository, publisher).ChangeStatusAsync(
            101,
            new UpdateComplaintStatusRequest { Status = "IN_PROGRESS" },
            20,
            ComplaintRoles.Warden);

        await publisher.Received(1).PublishStatusChangeNotificationAsync(
            Arg.Is<ComplaintStatusChangedEvent>(e =>
                e.ComplaintId == 101 &&
                e.StudentId == "500" &&
                e.OldStatus == "OPEN" &&
                e.NewStatus == "IN_PROGRESS" &&
                e.Timestamp == Now.UtcDateTime));
    }

    [Fact]
    public async Task ChangeStatus_ForAMissingComplaint_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<ComplaintNotFoundException>(
            () => CreateService(new FakeComplaintRepository()).ChangeStatusAsync(
                404,
                new UpdateComplaintStatusRequest { Status = "RESOLVED" },
                20,
                ComplaintRoles.Warden));
    }

    [Fact]
    public async Task ChangeStatus_AcceptsStatusInAnyCaseWithSpaces()
    {
        var repository = new FakeComplaintRepository();
        var complaint = repository.Seed(7);

        var result = await CreateService(repository).ChangeStatusAsync(
            complaint.ComplaintId,
            new UpdateComplaintStatusRequest { Status = "  resolved " },
            20,
            ComplaintRoles.Warden);

        Assert.Equal(ComplaintStatuses.Resolved, result.Status);
    }

    [Fact]
    public async Task ChangeStatus_TrimsRemarksBeforeSavingThem()
    {
        var (repository, _) = CreateMockedRepository();

        await CreateService(repository).ChangeStatusAsync(
            101,
            new UpdateComplaintStatusRequest
            {
                Status = "IN_PROGRESS",
                Remarks = "  Plumber visited  "
            },
            20,
            ComplaintRoles.Warden);

        await repository.Received(1).TryChangeStatusAsync(
            101UL,
            "OPEN",
            "IN_PROGRESS",
            20UL,
            ComplaintRoles.Warden,
            "Plumber visited",
            Now.UtcDateTime);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ChangeStatus_TreatsBlankRemarksAsNone(string? remarks)
    {
        var (repository, _) = CreateMockedRepository();

        await CreateService(repository).ChangeStatusAsync(
            101,
            new UpdateComplaintStatusRequest
            {
                Status = "IN_PROGRESS",
                Remarks = remarks
            },
            20,
            ComplaintRoles.Warden);

        await repository.Received(1).TryChangeStatusAsync(
            101UL,
            "OPEN",
            "IN_PROGRESS",
            20UL,
            ComplaintRoles.Warden,
            null,
            Now.UtcDateTime);
    }

    [Fact]
    public async Task ChangeStatus_RemarksAtMaximumLength_AreAccepted()
    {
        var repository = new FakeComplaintRepository();
        var complaint = repository.Seed(7);

        var result = await CreateService(repository).ChangeStatusAsync(
            complaint.ComplaintId,
            new UpdateComplaintStatusRequest
            {
                Status = "IN_PROGRESS",
                Remarks = new string('a', StaffComplaintService.MaxRemarksLength)
            },
            20,
            ComplaintRoles.Warden);

        Assert.Equal(ComplaintStatuses.InProgress, result.Status);
    }

    [Fact]
    public async Task ChangeStatus_RemarksOverMaximumLength_AreRejectedAndNothingChanges()
    {
        var repository = new FakeComplaintRepository();
        var complaint = repository.Seed(7);

        var exception = await Assert.ThrowsAsync<ComplaintValidationException>(
            () => CreateService(repository).ChangeStatusAsync(
                complaint.ComplaintId,
                new UpdateComplaintStatusRequest
                {
                    Status = "IN_PROGRESS",
                    Remarks = new string(
                        'a', StaffComplaintService.MaxRemarksLength + 1)
                },
                20,
                ComplaintRoles.Warden));

        Assert.True(exception.Errors.ContainsKey("remarks"));
        Assert.Equal(ComplaintStatuses.Open, repository.Complaints[0].Status);
        Assert.Empty(repository.AuditEntries);
    }

    [Fact]
    public async Task ChangeStatus_WithInvalidStatusAndLongRemarks_ReportsBothErrors()
    {
        var repository = new FakeComplaintRepository();
        var complaint = repository.Seed(7);

        var exception = await Assert.ThrowsAsync<ComplaintValidationException>(
            () => CreateService(repository).ChangeStatusAsync(
                complaint.ComplaintId,
                new UpdateComplaintStatusRequest
                {
                    Status = "DONE",
                    Remarks = new string(
                        'a', StaffComplaintService.MaxRemarksLength + 1)
                },
                20,
                ComplaintRoles.Warden));

        Assert.True(exception.Errors.ContainsKey("status"));
        Assert.True(exception.Errors.ContainsKey("remarks"));
    }

    [Fact]
    public async Task Assign_WithAssigneeZero_IsRejectedAndNothingChanges()
    {
        var repository = new FakeComplaintRepository();
        var complaint = repository.Seed(7);

        var exception = await Assert.ThrowsAsync<ComplaintValidationException>(
            () => CreateService(repository).AssignAsync(
                complaint.ComplaintId,
                new AssignComplaintRequest { AssignedToUserId = 0 },
                20,
                ComplaintRoles.Warden));

        Assert.True(exception.Errors.ContainsKey("assignedToUserId"));
        Assert.Null(repository.Complaints[0].AssignedToUserId);
        Assert.Empty(repository.AuditEntries);
    }

    [Fact]
    public async Task Assign_DoesNotChangeTheComplaintStatus()
    {
        var repository = new FakeComplaintRepository();
        var complaint = repository.Seed(7, ComplaintStatuses.InProgress);

        var result = await CreateService(repository).AssignAsync(
            complaint.ComplaintId,
            new AssignComplaintRequest { AssignedToUserId = 31 },
            20,
            ComplaintRoles.Warden);

        Assert.Equal(ComplaintStatuses.InProgress, result.Status);
    }

    [Fact]
    public async Task Assign_RecordsTheActingRoleInTheAuditEntry()
    {
        var repository = new FakeComplaintRepository();
        var complaint = repository.Seed(7);

        await CreateService(repository).AssignAsync(
            complaint.ComplaintId,
            new AssignComplaintRequest(),
            21,
            ComplaintRoles.HostelMaster);

        var audit = Assert.Single(repository.AuditEntries);

        Assert.Equal(ComplaintRoles.HostelMaster, audit.ActorRole);
        Assert.Equal((ulong)21, audit.ActorUserId);
    }

    [Fact]
    public async Task GetComplaints_WithUnknownCategory_IsRejected()
    {
        var exception = await Assert.ThrowsAsync<ComplaintValidationException>(
            () => CreateService(new FakeComplaintRepository())
                .GetComplaintsAsync(null, "nonsense"));

        Assert.True(exception.Errors.ContainsKey("category"));
    }

    [Fact]
    public async Task GetComplaints_TreatsBlankFiltersAsNoFilter()
    {
        var repository = new FakeComplaintRepository();
        repository.Seed(1, ComplaintStatuses.Open, "PLUMBING");
        repository.Seed(2, ComplaintStatuses.Resolved, "NOISE");

        var result = await CreateService(repository)
            .GetComplaintsAsync("  ", "");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task Report_ListsCategoryTotalsInAlphabeticalOrder()
    {
        var repository = new FakeComplaintRepository();
        repository.Seed(1, ComplaintStatuses.Open, "NOISE");
        repository.Seed(2, ComplaintStatuses.Open, "PLUMBING");
        repository.Seed(3, ComplaintStatuses.Open, "ELECTRICAL");
        repository.Seed(4, ComplaintStatuses.Open, "NOISE");

        var report = await CreateService(repository).GetReportAsync(null, null);

        Assert.Equal(
            new[] { "ELECTRICAL", "NOISE", "PLUMBING" },
            report.ByCategory.Select(total => total.Category).ToArray());

        Assert.Equal(
            2,
            report.ByCategory.Single(total => total.Category == "NOISE").Count);
    }

    [Fact]
    public async Task Report_CategoryFilter_NarrowsTotalsAndCategoryBreakdown()
    {
        var repository = new FakeComplaintRepository();
        repository.Seed(1, ComplaintStatuses.Open, "NOISE");
        repository.Seed(2, ComplaintStatuses.Resolved, "PLUMBING");

        var report = await CreateService(repository)
            .GetReportAsync(null, "plumbing");

        Assert.Equal(1, report.Totals.Total);
        Assert.Equal(1, report.Totals.Resolved);
        Assert.Equal("PLUMBING", Assert.Single(report.ByCategory).Category);
    }

    [Fact]
    public async Task Report_WithNoComplaints_ReturnsZeroTotalsAndEmptyLists()
    {
        var report = await CreateService(new FakeComplaintRepository())
            .GetReportAsync(null, null);

        Assert.Equal(0, report.Totals.Total);
        Assert.Empty(report.ByCategory);
        Assert.Empty(report.Complaints);
    }

    [Fact]
    public async Task Report_WithBothInvalidFilters_ReportsBothErrors()
    {
        var exception = await Assert.ThrowsAsync<ComplaintValidationException>(
            () => CreateService(new FakeComplaintRepository())
                .GetReportAsync("nonsense", "nonsense"));

        Assert.True(exception.Errors.ContainsKey("status"));
        Assert.True(exception.Errors.ContainsKey("category"));
    }

    private static async Task<string> GenerateCsvAsync(
        FakeComplaintRepository repository,
        string? status = null,
        string? category = null)
    {
        byte[] bytes = await CreateService(repository)
            .GenerateReportCsvAsync(status, category);

        return Encoding.UTF8.GetString(bytes);
    }

    [Fact]
    public async Task Csv_StartsWithAUtf8ByteOrderMarkForExcel()
    {
        byte[] bytes = await CreateService(new FakeComplaintRepository())
            .GenerateReportCsvAsync(null, null);

        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray());
    }

    [Fact]
    public async Task Csv_HasTheExpectedHeaderColumns()
    {
        string csv = await GenerateCsvAsync(new FakeComplaintRepository());

        Assert.Contains(
            "Complaint ID,Student,Category,Description,Status," +
            "Assigned To,Created At,Resolved At",
            csv);
    }

    [Fact]
    public async Task Csv_ShowsUnassignedComplaintsAndFormatsDates()
    {
        var repository = new FakeComplaintRepository();
        repository.Seed(7, ComplaintStatuses.Resolved);

        string csv = await GenerateCsvAsync(repository);

        Assert.Contains("Unassigned", csv);
        Assert.Contains("2026-10-01 08:00:00", csv);
    }

    [Fact]
    public async Task Csv_ShowsTheAssigneeUserId()
    {
        var repository = new FakeComplaintRepository();
        var complaint = repository.Seed(7);
        complaint.AssignedToUserId = 31;

        string csv = await GenerateCsvAsync(repository);

        Assert.DoesNotContain("Unassigned", csv);
        Assert.Contains(",31,", csv);
    }

    [Fact]
    public async Task Csv_LeavesResolvedAtEmptyForUnresolvedComplaints()
    {
        var repository = new FakeComplaintRepository();
        repository.Seed(7, ComplaintStatuses.Open);

        string csv = await GenerateCsvAsync(repository);
        string row = csv.Split('\n')[1].TrimEnd('\r');

        Assert.EndsWith(",", row);
    }

    [Fact]
    public async Task Csv_QuotesValuesContainingCommas()
    {
        var repository = new FakeComplaintRepository();
        repository.Seed(7).Description = "Leak, urgent";

        string csv = await GenerateCsvAsync(repository);

        Assert.Contains("\"Leak, urgent\"", csv);
    }

    [Fact]
    public async Task Csv_DoublesQuotationMarksInsideValues()
    {
        var repository = new FakeComplaintRepository();
        repository.Seed(7).Description = "He said \"help\"";

        string csv = await GenerateCsvAsync(repository);

        Assert.Contains("\"He said \"\"help\"\"\"", csv);
    }

    [Fact]
    public async Task Csv_QuotesValuesContainingLineBreaks()
    {
        var repository = new FakeComplaintRepository();
        repository.Seed(7).Description = "Line one\nLine two";

        string csv = await GenerateCsvAsync(repository);

        Assert.Contains("\"Line one\nLine two\"", csv);
    }

    [Theory]
    [InlineData("=1+1")]
    [InlineData("+cmd")]
    [InlineData("-cmd")]
    [InlineData("@SUM(A1)")]
    public async Task Csv_NeutralisesEveryFormulaPrefix(string description)
    {
        var repository = new FakeComplaintRepository();
        repository.Seed(7).Description = description;

        string csv = await GenerateCsvAsync(repository);

        Assert.Contains("'" + description, csv);
    }

    [Fact]
    public async Task Csv_DoesNotAlterOrdinaryText()
    {
        var repository = new FakeComplaintRepository();
        repository.Seed(7).Description = "Broken window";

        string csv = await GenerateCsvAsync(repository);

        Assert.Contains("Broken window", csv);
        Assert.DoesNotContain("'Broken window", csv);
    }

    [Fact]
    public async Task Csv_HonoursStatusAndCategoryFilters()
    {
        var repository = new FakeComplaintRepository();
        repository.Seed(1, ComplaintStatuses.Open, "PLUMBING");
        repository.Seed(2, ComplaintStatuses.Resolved, "PLUMBING");
        repository.Seed(3, ComplaintStatuses.Open, "NOISE");

        string csv = await GenerateCsvAsync(repository, "open", "plumbing");

        string[] lines = csv
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(2, lines.Length);
        Assert.Contains("student1", lines[1]);
    }

    [Fact]
    public async Task Csv_WithInvalidFilter_IsRejected()
    {
        await Assert.ThrowsAsync<ComplaintValidationException>(
            () => CreateService(new FakeComplaintRepository())
                .GenerateReportCsvAsync("nonsense", null));
    }

    [Fact]
    public async Task ChangeStatus_WhenPublisherThrows_StillSavesTheNotificationRow()
    {
        var (repository, _) = CreateMockedRepository();
        var publisher = Substitute.For<INotificationPublisher>();

        publisher.PublishStatusChangeNotificationAsync(
                Arg.Any<ComplaintStatusChangedEvent>())
            .ThrowsAsync(new InvalidOperationException("Kafka is down."));

        var result = await CreateService(repository, publisher).ChangeStatusAsync(
            101,
            new UpdateComplaintStatusRequest { Status = "RESOLVED" },
            20,
            ComplaintRoles.Warden);

        Assert.NotNull(result);

        await repository.Received(1).AddNotificationAsync(
            Arg.Any<Guid>(),
            101UL,
            500UL,
            Arg.Any<string>(),
            Arg.Any<DateTime>());
    }
}