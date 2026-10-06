const API_BASE_URL =
  import.meta.env.VITE_COMPLAINT_API_BASE_URL ??
  "http://localhost:8083";

export class ComplaintApiError extends Error {
  constructor(message, status, details = {}) {
    super(message);

    this.name = "ComplaintApiError";
    this.status = status;
    this.details = details;
  }
}

function getErrorMessage(data, fallback) {
  if (data.message) {
    return data.message;
  }

  if (data.errors) {
    const firstError = Object.values(data.errors)
      .flat()
      .find(Boolean);

    if (firstError) {
      return firstError;
    }
  }

  return fallback;
}

function endSession() {
  sessionStorage.clear();
  window.location.replace("/");
}

async function sendComplaintRequest(
  path,
  options = {},
  forbiddenMessage = "You are not authorized to use complaints."
) {
  const accessToken =
    sessionStorage.getItem("accessToken");

  const response = await fetch(
    `${API_BASE_URL}${path}`,
    {
      ...options,
      headers: {
        Authorization: `Bearer ${accessToken}`,
        ...(options.body
          ? { "Content-Type": "application/json" }
          : {}),
        ...options.headers
      }
    }
  );

  const data =
    response.status === 204
      ? {}
      : await response.json().catch(() => ({}));

  if (response.status === 401) {
    endSession();

    throw new ComplaintApiError(
      "Your session expired. Please sign in again.",
      401,
      data
    );
  }

  if (response.status === 403) {
    throw new ComplaintApiError(forbiddenMessage, 403, data);
  }

  if (!response.ok) {
    throw new ComplaintApiError(
      getErrorMessage(
        data,
        "The complaint request could not be completed."
      ),
      response.status,
      data
    );
  }

  return data;
}

function buildQuery(filters = {}) {
  const query = new URLSearchParams();

  if (filters.status) {
    query.set("status", filters.status);
  }

  if (filters.category) {
    query.set("category", filters.category);
  }

  const text = query.toString();

  return text ? `?${text}` : "";
}

/* ---------- Student (HMS-57) ---------- */

const studentMessage =
  "You are not authorized to manage your complaints.";

export function getMyComplaints() {
  return sendComplaintRequest(
    "/api/student/complaints",
    { method: "GET" },
    studentMessage
  );
}

export function getMyNotifications() {
  return sendComplaintRequest(
    "/api/student/complaints/notifications",
    { method: "GET" },
    studentMessage
  );
}

export function submitComplaint(complaint) {
  return sendComplaintRequest(
    "/api/student/complaints",
    {
      method: "POST",
      body: JSON.stringify({
        category: complaint.category,
        description: complaint.description.trim()
      })
    },
    studentMessage
  );
}

export function markNotificationRead(notificationId) {
  return sendComplaintRequest(
    `/api/student/complaints/notifications/${notificationId}/read`,
    { method: "PUT" },
    studentMessage
  );
}

/* ---------- Staff (HMS-58) ---------- */

const staffMessage =
  "You are not authorized to manage complaints.";

// Returns { generatedAtUtc, totals, byCategory, complaints }.
export function getComplaintReport(filters = {}) {
  return sendComplaintRequest(
    `/api/staff/complaints/report${buildQuery(filters)}`,
    { method: "GET" },
    staffMessage
  );
}

// assignedToUserId = null assigns the complaint to the caller.
export function assignComplaint(complaintId, assignedToUserId) {
  return sendComplaintRequest(
    `/api/staff/complaints/${complaintId}/assign`,
    {
      method: "PUT",
      body: JSON.stringify({
        assignedToUserId: assignedToUserId ?? null
      })
    },
    staffMessage
  );
}

export function changeComplaintStatus(
  complaintId,
  status,
  remarks
) {
  return sendComplaintRequest(
    `/api/staff/complaints/${complaintId}/status`,
    {
      method: "PUT",
      body: JSON.stringify({
        status,
        remarks: remarks?.trim() || null
      })
    },
    staffMessage
  );
}

export async function downloadComplaintReportCsv(filters = {}) {
  const accessToken =
    sessionStorage.getItem("accessToken");

  const response = await fetch(
    `${API_BASE_URL}/api/staff/complaints/report/csv${buildQuery(filters)}`,
    {
      headers: { Authorization: `Bearer ${accessToken}` }
    }
  );

  if (response.status === 401) {
    endSession();

    throw new ComplaintApiError(
      "Your session expired. Please sign in again.",
      401
    );
  }

  if (!response.ok) {
    throw new ComplaintApiError(
      "The report could not be downloaded.",
      response.status
    );
  }

  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");

  link.href = url;
  link.download =
    `complaint-report-${new Date().toISOString().slice(0, 10)}.csv`;

  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}