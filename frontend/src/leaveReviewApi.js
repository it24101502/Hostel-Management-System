import { sendLeaveRequest } from "./leaveHttp.js";

export { LeaveApiError } from "./leaveHttp.js";

const staffMessage =
  "You are not authorized to manage leave requests.";

// filters.status may be a real status or the special value
// "OVERDUE", which the API expresses as overdueOnly=true.
export function getLeaveReport(filters = {}) {
  const query = new URLSearchParams();

  if (filters.status === "OVERDUE") {
    query.set("overdueOnly", "true");
  } else if (filters.status) {
    query.set("status", filters.status);
  }

  if (filters.fromDate) {
    query.set("fromDate", filters.fromDate);
  }

  if (filters.toDate) {
    query.set("toDate", filters.toDate);
  }

  const suffix = query.toString();

  return sendLeaveRequest(
    `/api/staff/leave-requests/report${
      suffix ? `?${suffix}` : ""
    }`,
    { method: "GET" },
    staffMessage
  );
}

export function decideLeaveRequest(
  leaveRequestId,
  decision,
  reason
) {
  return sendLeaveRequest(
    `/api/staff/leave-requests/${leaveRequestId}/decision`,
    {
      method: "PUT",
      body: JSON.stringify({
        decision,
        reason: reason.trim()
      })
    },
    staffMessage
  );
}

export function recordDeparture(leaveRequestId) {
  return sendLeaveRequest(
    `/api/staff/leave-requests/${leaveRequestId}/departure`,
    { method: "PUT" },
    staffMessage
  );
}

export function recordReturn(leaveRequestId) {
  return sendLeaveRequest(
    `/api/staff/leave-requests/${leaveRequestId}/return`,
    { method: "PUT" },
    staffMessage
  );
}

export function getStaffNotifications(unreadOnly = true) {
  return sendLeaveRequest(
    `/api/staff/leave-notifications?unreadOnly=${unreadOnly}`,
    { method: "GET" },
    staffMessage
  );
}

export function markNotificationRead(notificationId) {
  return sendLeaveRequest(
    `/api/staff/leave-notifications/${notificationId}/read`,
    { method: "PUT" },
    staffMessage
  );
}
