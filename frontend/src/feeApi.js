const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ??
  "http://localhost:5220";

export class FeeApiError extends Error {
  constructor(message, status, details = {}) {
    super(message);
    this.name = "FeeApiError";
    this.status = status;
    this.details = details;
  }
}

export async function getMyFeeReminders() {
  const accessToken =
    sessionStorage.getItem("accessToken");

  const response = await fetch(
    `${API_BASE_URL}/api/student/fee-reminders`,
    {
      headers: { Authorization: `Bearer ${accessToken}` }
    }
  );

  const data =
    await response.json().catch(() => ({}));

  if (response.status === 401) {
    sessionStorage.clear();
    window.location.replace("/");

    throw new FeeApiError(
      "Your session expired. Please sign in again.",
      401,
      data
    );
  }

  if (response.status === 403) {
    throw new FeeApiError(
      "You are not authorized to view fee reminders.",
      403,
      data
    );
  }

  if (!response.ok) {
    throw new FeeApiError(
      data.message ??
        "The fee reminders could not be loaded.",
      response.status,
      data
    );
  }

  return data;
}