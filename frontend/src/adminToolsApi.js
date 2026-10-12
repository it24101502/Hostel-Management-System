// API calls for the admin fee screens, the login audit log and
// guardian / emergency contacts. All of these live in IdentityService.

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ??
  "http://localhost:5220";

export class ToolsApiError extends Error {
  constructor(message, status, details = {}) {
    super(message);
    this.name = "ToolsApiError";
    this.status = status;
    this.details = details;
  }
}

function getErrorMessage(data, fallback) {
  if (data.message) {
    return data.message;
  }

  // ASP.NET model validation: { errors: { Field: ["message"] } }
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

async function sendRequest(path, options = {}) {
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

  if (response.status === 401) {
    endSession();

    throw new ToolsApiError(
      "Your session expired. Please sign in again.",
      401
    );
  }

  return response;
}

async function sendJson(path, options = {}) {
  const response = await sendRequest(path, options);

  const data =
    response.status === 204
      ? {}
      : await response.json().catch(() => ({}));

  if (response.status === 403) {
    throw new ToolsApiError(
      "You are not authorized to do that.",
      403,
      data
    );
  }

  if (!response.ok) {
    throw new ToolsApiError(
      getErrorMessage(
        data,
        "The request could not be completed."
      ),
      response.status,
      data
    );
  }

  return data;
}

function buildQuery(params) {
  const query = new URLSearchParams();

  Object.entries(params).forEach(([key, value]) => {
    if (value !== "" && value != null) {
      query.set(key, value);
    }
  });

  const text = query.toString();

  return text ? `?${text}` : "";
}

/* ---------- Students (for pickers) ---------- */

export function getStudentProfiles() {
  return sendJson("/api/student-profiles");
}

export function getOwnProfile() {
  return sendJson("/api/student-profiles/me");
}

/* ---------- Fees ---------- */

export function getFeeReport(filters = {}) {
  return sendJson(
    `/api/admin/fee-reports${buildQuery(filters)}`
  );
}

export function createFeeInvoice(invoice) {
  return sendJson("/api/fee-invoices", {
    method: "POST",
    body: JSON.stringify(invoice)
  });
}

export function recordFeePayment(invoiceId, payment) {
  return sendJson(
    `/api/fee-invoices/${invoiceId}/payments`,
    {
      method: "POST",
      body: JSON.stringify(payment)
    }
  );
}

// The CSV endpoint needs the Bearer token, so a plain link will
// not work. Fetch it and hand the browser a temporary file.
export async function downloadFeeReportCsv(filters = {}) {
  const response = await sendRequest(
    `/api/admin/fee-reports/csv${buildQuery(filters)}`
  );

  if (!response.ok) {
    const data = await response
      .json()
      .catch(() => ({}));

    throw new ToolsApiError(
      getErrorMessage(
        data,
        "The report could not be downloaded."
      ),
      response.status,
      data
    );
  }

  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");

  link.href = url;
  link.download = `fee-status-report-${new Date()
    .toISOString()
    .slice(0, 10)}.csv`;

  document.body.appendChild(link);
  link.click();
  link.remove();

  URL.revokeObjectURL(url);
}

/* ---------- Login audit log ---------- */

export function getLoginAuditLogs(limit = 100) {
  return sendJson(
    `/api/admin/auth-audit-logs${buildQuery({ limit })}`
  );
}

/* ---------- Guardian and emergency contacts ---------- */

export function getContacts(studentProfileId) {
  return sendJson(
    `/api/student-profiles/${studentProfileId}/contacts`
  );
}

export function createContact(studentProfileId, contact) {
  return sendJson(
    `/api/student-profiles/${studentProfileId}/contacts`,
    {
      method: "POST",
      body: JSON.stringify(contact)
    }
  );
}

export function updateContact(
  studentProfileId,
  contactId,
  contact
) {
  return sendJson(
    `/api/student-profiles/${studentProfileId}/contacts/${contactId}`,
    {
      method: "PUT",
      body: JSON.stringify(contact)
    }
  );
}
