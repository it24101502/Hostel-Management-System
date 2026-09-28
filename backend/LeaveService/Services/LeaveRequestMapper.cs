using LeaveService.DTOs;
using LeaveService.Models;

namespace LeaveService.Services;

public static class LeaveRequestMapper
{
    public static LeaveRequestResponse ToResponse(
        LeaveRequest request)
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
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt
        };
    }
}
