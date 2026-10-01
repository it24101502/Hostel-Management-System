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
        var errors = Validate(request);

        if (errors.Count > 0)
        {
            throw new ComplaintValidationException(errors);
        }

        // Validate guarantees these values are present and valid.
        var newComplaint = new NewComplaint
        {
            StudentUserId = studentUserId,
            StudentUsername = studentUsername,
            Category = request.Category!.Trim().ToUpperInvariant(),
            Description = request.Description!.Trim()
        };

        Complaint created =
            await _repository.CreateAsync(
                newComplaint,
                _timeProvider.GetUtcNow().UtcDateTime);

        return MapResponse(created);
    }

    public async Task<IReadOnlyList<ComplaintResponse>>
        GetMyComplaintsAsync(ulong studentUserId)
    {
        var complaints =
            await _repository.GetByStudentAsync(studentUserId);

        return complaints.Select(MapResponse).ToList();
    }

    public async Task<ComplaintResponse?> GetMyComplaintAsync(
        ulong complaintId,
        ulong studentUserId)
    {
        var complaint =
            await _repository.GetByIdAsync(complaintId);

        // Another student's complaint is reported as "not found"
        // so its existence is not revealed.
        if (complaint is null ||
            complaint.StudentUserId != studentUserId)
        {
            return null;
        }

        return MapResponse(complaint);
    }

    private static Dictionary<string, string[]> Validate(
        SubmitComplaintRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        string category =
            request.Category?.Trim().ToUpperInvariant() ?? string.Empty;

        if (category.Length == 0)
        {
            errors["category"] = new[]
            {
                "Category is required."
            };
        }
        else if (!ComplaintCategories.All.Contains(category))
        {
            errors["category"] = new[]
            {
                "Category must be one of: " +
                string.Join(", ", ComplaintCategories.All) + "."
            };
        }

        string description = request.Description?.Trim() ?? string.Empty;

        if (description.Length == 0)
        {
            errors["description"] = new[]
            {
                "Description is required."
            };
        }
        else if (description.Length > MaxDescriptionLength)
        {
            errors["description"] = new[]
            {
                $"Description cannot exceed {MaxDescriptionLength} characters."
            };
        }

        return errors;
    }

    private static ComplaintResponse MapResponse(
        Complaint complaint)
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
