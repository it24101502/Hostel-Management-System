const API_BASE_URL =
  import.meta.env.VITE_LEAVE_API_BASE_URL ??
  "http://localhost:8082";

export class LeaveApiError extends Error {
  constructor(message, status, details = {}) {
    super(message);

    this.name = "LeaveApiError";
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

export async function sendLeaveRequest(
  path,
  options = {},
  forbiddenMessage =
    "You are not authorized to use leave requests."
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
    sessionStorage.clear();
    window.location.replace("/");

    throw new LeaveApiError(
      "Your session expired. Please sign in again.",
      401,
      data
    );
  }

  if (response.status === 403) {
    throw new LeaveApiError(
      forbiddenMessage,
      403,
      data
    );
  }

  if (!response.ok) {
    throw new LeaveApiError(
      getErrorMessage(
        data,
        "The leave request could not be completed."
      ),
      response.status,
      data
    );
  }

  return data;
}
