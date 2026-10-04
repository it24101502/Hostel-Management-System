using ComplaintService.DTOs;
using ComplaintService.Exceptions;
using ComplaintService.Models;
using ComplaintService.Repositories;

namespace ComplaintService.Services;

public class StaffComplaintService : IStaffComplaintService
{
    public const int MaxRemarksLength = 500;

    private static readonly string[] ValidStatuses =
    {
        ComplaintStatuses.Open,
        ComplaintStatuses.InProgress,
        ComplaintStatuses.Resolved
    };

    private readonly IComplaintRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly INotificationPublisher _notificationPublisher;

    public StaffComplaintService(
        IComplaintRepository repository,
        TimeProvider timeProvider,
        INotificationPublisher notificationPublisher)
    {
        _repository = repository;
        _timeProvider = timeProvider;
        _notificationPublisher = notificationPublisher;
    }

    public async Task<IReadOnlyList<ComplaintResponse>> GetComplaintsAsync(
        string? status, string? category)
    {
        var errors = new Dictionary<string, string[]>();

        string? cleanStatus = Normalise(status);
        string? cleanCategory = Normalise(category);

        if (cleanStatus is not null && !ValidStatuses.Contains(cleanStatus))
        {
            errors["status"] = new[]
            {
                "Status must be OPEN, IN_PROGRESS or RESOLVED."
            };
        }

        if (cleanCategory is not null &&
            !ComplaintCategories.All.Contains(cleanCategory))
        {
            errors["category"] = new[] { "Unknown category." };
        }

        if (errors.Count > 0)
        {
            throw new ComplaintValidationException(errors);
        }

        var complaints =
            await _repository.GetFilteredAsync(cleanStatus, cleanCategory);

        return complaints.Select(ToResponse).ToList();
    }

    public async Task<ComplaintResponse> AssignAsync(
        ulong complaintId,
        AssignComplaintRequest request,
        ulong actorUserId,
        string actorRole)
    {
        ulong assignee = request.AssignedToUserId ?? actorUserId;

        if (assignee == 0)
        {
            throw new ComplaintValidationException(
                new Dictionary<string, string[]>
                {
                    ["assignedToUserId"] = new[]
                    {
                        "A valid staff user ID is required."
                    }
                });
        }

        await GetExistingAsync(complaintId);

        await _repository.AssignAsync(
            complaintId,
            assignee,
            actorUserId,
            actorRole,
            _timeProvider.GetUtcNow().UtcDateTime);

        return ToResponse(await GetExistingAsync(complaintId));
    }

    public async Task<ComplaintResponse> ChangeStatusAsync(
        ulong complaintId,
        UpdateComplaintStatusRequest request,
        ulong actorUserId,
        string actorRole)
    {
        var errors = new Dictionary<string, string[]>();

        string? newStatus = Normalise(request.Status);

        if (newStatus is null || !ValidStatuses.Contains(newStatus))
        {
            errors["status"] = new[]
            {
                "Status must be OPEN, IN_PROGRESS or RESOLVED."
            };
        }

        string? remarks = string.IsNullOrWhiteSpace(request.Remarks)
            ? null
            : request.Remarks.Trim();

        if (remarks is not null && remarks.Length > MaxRemarksLength)
        {
            errors["remarks"] = new[]
            {
                $"Remarks cannot exceed {MaxRemarksLength} characters."
            };
        }

        if (errors.Count > 0)
        {
            throw new ComplaintValidationException(errors);
        }

        Complaint existing = await GetExistingAsync(complaintId);

        if (existing.Status == newStatus)
        {
            throw new InvalidComplaintStatusException(
                $"The complaint is already {existing.Status.ToLowerInvariant()}.");
        }

        string oldStatus = existing.Status;
        DateTime nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        bool applied = await _repository.TryChangeStatusAsync(
            complaintId,
            oldStatus,
            newStatus!,
            actorUserId,
            actorRole,
            remarks,
            nowUtc);

        if (applied)
        {
            string message = $"Your complaint #{complaintId} status changed from {oldStatus} to {newStatus}.";

            await _repository.AddNotificationAsync(
                Guid.NewGuid(),
                complaintId,
                existing.StudentUserId,
                message,
                nowUtc
            );

            // Publish Kafka / Background Event
            var notificationEvent = new ComplaintStatusChangedEvent
            {
                ComplaintId = existing.ComplaintId,
                StudentId = existing.StudentUserId.ToString(),
                OldStatus = oldStatus,
                NewStatus = newStatus!,
                Timestamp = nowUtc
            };

            await _notificationPublisher.PublishStatusChangeNotificationAsync(notificationEvent);
        }

        return ToResponse(await GetExistingAsync(complaintId));
    }

    private async Task<Complaint> GetExistingAsync(ulong complaintId)
    {
        return await _repository.GetByIdAsync(complaintId)
            ?? throw new ComplaintNotFoundException();
    }

    private static string? Normalise(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToUpperInvariant();
    }

    private static ComplaintResponse ToResponse(Complaint complaint)
    {
        return new ComplaintResponse
        {
            ComplaintId = complaint.ComplaintId,
            StudentUserId = complaint.StudentUserId,
            StudentUsername = complaint.StudentUsername,
            Category = complaint.Category,
            Description = complaint.Description,
            Status = complaint.Status,
            AssignedToUserId = complaint.AssignedToUserId,
            AssignedAt = complaint.AssignedAt,
            ResolvedAt = complaint.ResolvedAt,
            CreatedAt = complaint.CreatedAt,
            UpdatedAt = complaint.UpdatedAt
        };
    }
}