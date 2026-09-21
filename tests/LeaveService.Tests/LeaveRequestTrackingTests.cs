using LeaveService.Models;
using LeaveService.Tests.TestDoubles;

namespace LeaveService.Tests;

public class LeaveRequestTrackingTests
{
    [Fact]
    public async Task GetMyRequestsAsync_ReturnsOnlyTheStudentsRequests()
    {
        var repository = new FakeLeaveRequestRepository();
        repository.Seed(studentUserId: 7);
        repository.Seed(studentUserId: 8);
        repository.Seed(studentUserId: 7);

        var service = LeaveTestData.CreateRequestService(
            repository,
            new FakeLeaveEventPublisher());

        var result = await service.GetMyRequestsAsync(7);

        Assert.Equal(2, result.Count);
        Assert.All(result, request => Assert.Equal((ulong)7, request.StudentUserId));
    }

    [Fact]
    public async Task GetMyRequestsAsync_WhenStudentHasNoRequests_ReturnsEmptyList()
    {
        var repository = new FakeLeaveRequestRepository();
        repository.Seed(studentUserId: 8);

        var service = LeaveTestData.CreateRequestService(
            repository,
            new FakeLeaveEventPublisher());

        var result = await service.GetMyRequestsAsync(7);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMyRequestsAsync_ReturnsTheCurrentStatusOfEachRequest()
    {
        var repository = new FakeLeaveRequestRepository();
        repository.Seed(7, LeaveRequestStatuses.Pending);
        repository.Seed(7, LeaveRequestStatuses.Approved);
        repository.Seed(7, LeaveRequestStatuses.Rejected);
        repository.Seed(7, LeaveRequestStatuses.Closed);

        var service = LeaveTestData.CreateRequestService(
            repository,
            new FakeLeaveEventPublisher());

        var result = await service.GetMyRequestsAsync(7);

        var statuses = result.Select(request => request.Status).ToList();

        Assert.Equal(4, statuses.Count);
        Assert.Contains(LeaveRequestStatuses.Pending, statuses);
        Assert.Contains(LeaveRequestStatuses.Approved, statuses);
        Assert.Contains(LeaveRequestStatuses.Rejected, statuses);
        Assert.Contains(LeaveRequestStatuses.Closed, statuses);
    }

    [Fact]
    public async Task GetMyRequestAsync_ForOwnRequest_ReturnsIt()
    {
        var repository = new FakeLeaveRequestRepository();
        var own = repository.Seed(studentUserId: 7);

        var service = LeaveTestData.CreateRequestService(
            repository,
            new FakeLeaveEventPublisher());

        var result = await service.GetMyRequestAsync(own.LeaveRequestId, 7);

        Assert.NotNull(result);
        Assert.Equal(own.LeaveRequestId, result.LeaveRequestId);
    }

    [Fact]
    public async Task GetMyRequestAsync_ForAnotherStudentsRequest_ReturnsNull()
    {
        var repository = new FakeLeaveRequestRepository();
        var other = repository.Seed(studentUserId: 8);

        var service = LeaveTestData.CreateRequestService(
            repository,
            new FakeLeaveEventPublisher());

        var result = await service.GetMyRequestAsync(other.LeaveRequestId, 7);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetMyRequestAsync_WhenRequestDoesNotExist_ReturnsNull()
    {
        var service = LeaveTestData.CreateRequestService(
            new FakeLeaveRequestRepository(),
            new FakeLeaveEventPublisher());

        var result = await service.GetMyRequestAsync(404, 7);

        Assert.Null(result);
    }
}
