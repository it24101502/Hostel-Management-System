const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ??
  "http://localhost:5220";

export class ApiError extends Error {
  constructor(message, status, details = {}) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.details = details;
  }
}

async function sendRequest(path, options = {}) {
  const accessToken =
    sessionStorage.getItem("accessToken");

  const response = await fetch(
    `${API_BASE_URL}${path}`,
    {
      ...options,
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${accessToken}`,
        ...options.headers
      }
    }
  );

  const data =
    await response.json().catch(() => ({}));

  if (response.status === 401) {
    sessionStorage.clear();
    window.location.replace("/");

    throw new ApiError(
      "Your session has expired. Please sign in again.",
      401,
      data
    );
  }

  if (!response.ok) {
    throw new ApiError(
      data.message ??
        "The request could not be completed.",
      response.status,
      data
    );
  }

  return data;
}

export function getOwnStudentProfile() {
  return sendRequest(
    "/api/student-profiles/me",
    {
      method: "GET"
    }
  );
}

export function updateOwnStudentProfile(profile) {
  return sendRequest(
    "/api/student-profiles/me",
    {
      method: "PUT",
      body: JSON.stringify({
        addressLine1: profile.addressLine1 || null,
        addressLine2: profile.addressLine2 || null,
        city: profile.city || null,
        district: profile.district || null,
        postalCode: profile.postalCode || null,
      })
    }
  );
}

export async function uploadOwnStudentPhoto(photo) {
  const accessToken =
    sessionStorage.getItem("accessToken");

  const formData = new FormData();

  formData.append("photo", photo);

  const response = await fetch(
    `${API_BASE_URL}/api/student-profiles/me/photo`,
    {
      method: "POST",
      headers: {
        Authorization: `Bearer ${accessToken}`
      },
      body: formData
    }
  );

  const data =
    await response.json().catch(() => ({}));

  if (response.status === 401) {
    sessionStorage.clear();
    window.location.replace("/");

    throw new ApiError(
      "Your session has expired. Please sign in again.",
      401,
      data
    );
  }

  if (!response.ok) {
    throw new ApiError(
      data.message ??
        "The photo could not be uploaded.",
      response.status,
      data
    );
  }

  return data;
}