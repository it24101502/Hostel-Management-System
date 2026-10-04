using ComplaintService.Models;
using ComplaintService.DTOs;

namespace ComplaintService.Repositories;

public interface IComplaintRepository
{
    /// <summary>
    /// Saves a new OPEN complaint and its SUBMIT audit row in
    /// one database transaction.
    /// </summary>
    Task<Complaint> CreateAsync(
        NewComplaint complaint,
        DateTime occurredAtUtc);

    Task<Complaint?> GetByIdAsync(
        ulong complaintId);

    Task<IReadOnlyList<Complaint>> GetByStudentAsync(
        ulong studentUserId);

        Task<IReadOnlyList<Complaint>> GetFilteredAsync(
        string? status,
        string? category);

    /// <summary>Sets the assignee and records an ASSIGN audit row.</summary>
    Task AssignAsync(
        ulong complaintId,
        ulong assigneeUserId,
        ulong actorUserId,
        string actorRole,
        DateTime occurredAtUtc);

    /// <summary>
    /// Changes the status only if it is still fromStatus.
    /// Returns false if someone else changed it first.
    /// </summary>
    Task<bool> TryChangeStatusAsync(
        ulong complaintId,
        string fromStatus,
        string toStatus,
        ulong actorUserId,
        string actorRole,
        string? remarks,
        DateTime occurredAtUtc);

    /// <summary>
    /// Inserts a student notification when a complaint status changes.
    /// </summary>
    Task AddNotificationAsync(
        Guid notificationId,
        ulong complaintId,
        ulong studentId,
        string message,
        DateTime createdAtUtc);

    Task<IReadOnlyList<StudentNotificationResponse>> GetNotificationsByStudentIdAsync(ulong studentId);
}
