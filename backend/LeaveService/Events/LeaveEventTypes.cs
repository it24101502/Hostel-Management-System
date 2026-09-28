namespace LeaveService.Events;

public static class LeaveEventTypes
{
    public const string RequestSubmitted = "LEAVE_REQUEST_SUBMITTED";

    public const string RequestApproved = "LEAVE_REQUEST_APPROVED";

    public const string RequestRejected = "LEAVE_REQUEST_REJECTED";

    public const string StudentDeparted = "LEAVE_STUDENT_DEPARTED";

    public const string StudentReturned = "LEAVE_STUDENT_RETURNED";

    public const string ReturnOverdue = "LEAVE_RETURN_OVERDUE";
}
