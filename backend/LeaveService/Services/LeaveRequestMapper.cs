using LeaveService.DTOs;
using LeaveService.Models;

namespace LeaveService.Services;

public static class LeaveRequestMapper
{
    public static LeaveRequestResponse ToResponse(
        LeaveRequest request,
        DateOnly today)
    {
        return new LeaveRequestResponse
        {
            LeaveRequestId = request.LeaveRequestId,
            StudentUserId = request.StudentUserId,
            StudentUsername = request.StudentUsername,
            DepartureDate = request.DepartureDate,
            ExpectedReturnDate = request.ExpectedReturnDate,
            Reason = request.Reason,
            CompanionName = request.CompanionName,
            CompanionRelationship = request.CompanionRelationship,
            CompanionPhone = request.CompanionPhone,
            Status = request.Status,
            DecisionReason = request.DecisionReason,
            DecidedAt = request.DecidedAt,
            ActualDepartureAt = request.ActualDepartureAt,
            ActualReturnAt = request.ActualReturnAt,
            IsOverdue = IsOverdue(request, today),
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt
        };
    }

    /// <summary>
    /// A request is overdue when the student has departed and the
    /// expected return date has passed without a recorded return.
    /// The same rule is used by the report and the overdue job.
    /// </summary>
    public static bool IsOverdue(
        LeaveRequest request,
        DateOnly today)
    {
        return request.Status == LeaveRequestStatuses.Departed &&
               request.ExpectedReturnDate < today;
    }
}
