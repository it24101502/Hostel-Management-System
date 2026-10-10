using System.Globalization;
using System.Text;
using ComplaintService.DTOs;
using ComplaintService.Exceptions;
using ComplaintService.Models;
using ComplaintService.Repositories;
using Microsoft.Extensions.Logging;

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
    private readonly IStaffDirectory _staffDirectory;
    private readonly ILogger<StaffComplaintService>? _logger;

    public StaffComplaintService(
        IComplaintRepository repository,
        TimeProvider timeProvider,
        INotificationPublisher notificationPublisher,
        IStaffDirectory staffDirectory,
        ILogger<StaffComplaintService>? logger = null)
    {
        _staffDirectory = staffDirectory;
        _repository = repository;
        _timeProvider = timeProvider;
        _notificationPublisher = notificationPublisher;
        _logger = logger;
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

        // The signed-in user is already staff. Anyone else must be
        // an active warden, hostel master or admin.
        if (assignee != actorUserId &&
            !await _staffDirectory.IsActiveStaffAsync(assignee))
        {
            throw new ComplaintValidationException(
                new Dictionary<string, string[]>
                {
                    ["assignedToUserId"] = new[]
                    {
                        "The selected user is not an active staff member."
                    }
                });
        }

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

        if (!applied)
        {
            throw new InvalidComplaintStatusException(
                "Failed to update status. A concurrent update occurred or the complaint state was modified.");
        }

        string message =
            $"Your complaint #{complaintId} is now {Describe(newStatus!)} (was {Describe(oldStatus)}).";

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

        try
        {
            await _notificationPublisher.PublishStatusChangeNotificationAsync(notificationEvent);
        }
        catch (Exception exception)
        {
            // The status change and the notification row are already saved.
            _logger?.LogWarning(exception,
                "Unable to publish status change for complaint {ComplaintId}.",
                existing.ComplaintId);
        }

        return ToResponse(await GetExistingAsync(complaintId));
    }

    // ================= NEW FOR HMS-56 (start) =================

    public async Task<ComplaintReportResponse> GetReportAsync(
        string? status, string? category)
    {
        var (cleanStatus, cleanCategory) = ValidateFilters(status, category);

        // One query: everything in the chosen category.
        var inCategory =
            await _repository.GetFilteredAsync(null, cleanCategory);

        var matching = cleanStatus is null
            ? inCategory
            : inCategory.Where(c => c.Status == cleanStatus).ToList();

        return new ComplaintReportResponse
        {
            GeneratedAtUtc = _timeProvider.GetUtcNow(),
            Totals = new ComplaintReportTotals
            {
                Total = inCategory.Count,
                Open = inCategory.Count(c => c.Status == ComplaintStatuses.Open),
                InProgress = inCategory.Count(c => c.Status == ComplaintStatuses.InProgress),
                Resolved = inCategory.Count(c => c.Status == ComplaintStatuses.Resolved)
            },
            ByCategory = inCategory
                .GroupBy(c => c.Category)
                .Select(g => new ComplaintCategoryTotal
                {
                    Category = g.Key,
                    Count = g.Count()
                })
                .OrderBy(t => t.Category)
                .ToList(),
            Complaints = matching.Select(ToResponse).ToList()
        };
    }

    public async Task<byte[]> GenerateReportCsvAsync(
        string? status, string? category)
    {
        var report = await GetReportAsync(status, category);

        var csv = new StringBuilder();
        csv.AppendLine(
            "Complaint ID,Student,Category,Description,Status," +
            "Assigned To,Created At,Resolved At");

        foreach (var c in report.Complaints)
        {
            string[] values =
            [
                c.ComplaintId.ToString(CultureInfo.InvariantCulture),
                c.StudentUsername,
                c.Category,
                c.Description,
                c.Status,
                c.AssignedToUserId?.ToString(CultureInfo.InvariantCulture) ?? "Unassigned",
                c.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                c.ResolvedAt?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? ""
            ];

            csv.AppendLine(string.Join(",", values.Select(EscapeCsv)));
        }

        // UTF-8 BOM so Excel shows text correctly.
        byte[] preamble = Encoding.UTF8.GetPreamble();
        byte[] content = Encoding.UTF8.GetBytes(csv.ToString());
        return preamble.Concat(content).ToArray();
    }

    private static (string? Status, string? Category) ValidateFilters(
        string? status, string? category)
    {
        var errors = new Dictionary<string, string[]>();

        string? cleanStatus = Normalise(status);
        string? cleanCategory = Normalise(category);

        if (cleanStatus is not null && !ValidStatuses.Contains(cleanStatus))
        {
            errors["status"] = new[] { "Status must be OPEN, IN_PROGRESS or RESOLVED." };
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

        return (cleanStatus, cleanCategory);
    }

    private static string EscapeCsv(string? value)
    {
        string safe = value ?? string.Empty;

        // Stop spreadsheet formula injection (=, +, -, @ at the start).
        if (safe.Length > 0 && "=+-@".Contains(safe[0]))
        {
            safe = "'" + safe;
        }

        bool needsQuotes = safe.Contains(',') || safe.Contains('"') ||
                           safe.Contains('\r') || safe.Contains('\n');

        return needsQuotes ? $"\"{safe.Replace("\"", "\"\"")}\"" : safe;
    }

    // ================= NEW FOR HMS-56 (end) =================

    private static string Describe(string status) => status switch
    {
        ComplaintStatuses.Open => "open",
        ComplaintStatuses.InProgress => "in progress",
        ComplaintStatuses.Resolved => "resolved",
        _ => status.ToLowerInvariant()
    };

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