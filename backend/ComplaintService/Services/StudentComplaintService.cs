using ComplaintService.DTOs;
using ComplaintService.Exceptions;
using ComplaintService.Models;
using ComplaintService.Repositories;

namespace ComplaintService.Services;

public class StudentComplaintService : IStudentComplaintService
{
    public const int MaxDescriptionLength = 1000;

    private readonly IComplaintRepository _repository;
    private readonly TimeProvider _timeProvider;

    public StudentComplaintService(
        IComplaintRepository repository,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<ComplaintResponse> SubmitAsync(
        SubmitComplaintRequest request,
        ulong studentUserId,
        string studentUsername)
    {
        var errors = new Dictionary<string, string[]>();

        string? cleanCategory = Normalise(request.Category);
        string? cleanDescription = request.Description?.Trim();

        if (cleanCategory is null || !ComplaintCategories.All.Contains(cleanCategory))
        {
            errors["category"] = new[] { "Unknown category." };
        }

        if (string.IsNullOrWhiteSpace(cleanDescription))
        {
            errors["description"] = new[] { "Description is required." };
        }
        else if (cleanDescription.Length > MaxDescriptionLength)
        {
            errors["description"] = new[]
            {
                $"Description cannot exceed {MaxDescriptionLength} characters."
            };
        }

        if (errors.Count > 0)
        {
            throw new ComplaintValidationException(errors);
        }

        var newComplaint = new NewComplaint
        {
            StudentUserId = studentUserId,
            StudentUsername = studentUsername,
            Category = cleanCategory!,
            Description = cleanDescription!
        };

        DateTime nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var created = await _repository.CreateAsync(newComplaint, nowUtc);

        return ToResponse(created);
    }

    public async Task<IReadOnlyList<ComplaintResponse>> GetMyComplaintsAsync(
        ulong studentUserId)
    {
        var complaints = await _repository.GetByStudentAsync(studentUserId);
        return complaints.Select(ToResponse).ToList();
    }

    public async Task<ComplaintResponse?> GetMyComplaintAsync(
        ulong complaintId,
        ulong studentUserId)
    {
        var complaint = await _repository.GetByIdAsync(complaintId);

        if (complaint is null || complaint.StudentUserId != studentUserId)
        {
            return null;
        }

        return ToResponse(complaint);
    }

    public async Task<IReadOnlyList<StudentNotificationResponse>> GetNotificationsByStudentIdAsync(
        ulong studentId)
    {
        return await _repository.GetNotificationsByStudentIdAsync(studentId);
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