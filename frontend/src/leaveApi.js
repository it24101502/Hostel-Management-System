import { sendLeaveRequest } from "./leaveHttp.js";

export { LeaveApiError } from "./leaveHttp.js";

const studentMessage =
  "You are not authorized to manage your leave requests.";

export function getMyLeaveRequests() {
  return sendLeaveRequest(
    "/api/student/leave-requests",
    { method: "GET" },
    studentMessage
  );
}

export function submitLeaveRequest(leaveRequest) {
  return sendLeaveRequest(
    "/api/student/leave-requests",
    {
      method: "POST",
      body: JSON.stringify({
        departureDate: leaveRequest.departureDate,
        expectedReturnDate:
          leaveRequest.expectedReturnDate,
        reason: leaveRequest.reason.trim(),
        companionName:
          leaveRequest.companionName.trim(),
        companionRelationship:
          leaveRequest.companionRelationship.trim(),
        companionPhone:
          leaveRequest.companionPhone.trim()
      })
    },
    studentMessage
  );
}
