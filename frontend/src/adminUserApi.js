const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ??
  "http://localhost:5220";

export class AdminApiError extends Error {
  constructor(message, status, details = {}) {
    super(message);

    this.name = "AdminApiError";
    this.status = status;
    this.details = details;
  }
}

async function sendAdminRequest(
  path,
  options = {}
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
    await response.json().catch(() => ({}));

  if (response.status === 401) {
    sessionStorage.clear();
    window.location.replace("/");

    throw new AdminApiError(
      "Your session expired. Please sign in again.",
      401,
      data
    );
  }

  if (response.status === 403) {
    throw new AdminApiError(
      "You are not authorized to manage users.",
      403,
      data
    );
  }

  if (!response.ok) {
    throw new AdminApiError(
      data.message ??
        "The request could not be completed.",
      response.status,
      data
    );
  }

  return data;
}

export function getAdminUsers() {
  return sendAdminRequest(
    "/api/admin/users",
    {
      method: "GET"
    }
  );
}

export function getAdminUser(userId) {
  return sendAdminRequest(
    `/api/admin/users/${userId}`,
    {
      method: "GET"
    }
  );
}

export function createAdminUser(user) {
  return sendAdminRequest(
    "/api/admin/users",
    {
      method: "POST",
      body: JSON.stringify({
        username: user.username.trim(),
        email: user.email.trim(),
        firstName: user.firstName.trim(),
        lastName: user.lastName.trim(),
        phoneNumber:
          user.phoneNumber.trim() || null,
        password: user.password,
        roleId: Number(user.roleId)
      })
    }
  );
}

export function updateAdminUser(
  userId,
  user
) {
  return sendAdminRequest(
    `/api/admin/users/${userId}`,
    {
      method: "PUT",
      body: JSON.stringify({
        username: user.username.trim(),
        email: user.email.trim(),
        firstName: user.firstName.trim(),
        lastName: user.lastName.trim(),
        phoneNumber:
          user.phoneNumber.trim() || null,
        roleId: Number(user.roleId)
      })
    }
  );
}

export function deactivateAdminUser(userId) {
  return sendAdminRequest(
    `/api/admin/users/${userId}/deactivate`,
    {
      method: "PATCH"
    }
  );
}