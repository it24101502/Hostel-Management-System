const API_BASE_URL =
  import.meta.env.VITE_ACCOMMODATION_API_BASE_URL ??
  "http://localhost:8081";

export class AllocationApiError extends Error {
  constructor(message, status, details = {}) {
    super(message);
    this.name = "AllocationApiError";
    this.status = status;
    this.details = details;
  }
}

function getErrorMessage(data, fallback) {
  if (data.message) return data.message;

  if (data.errors) {
    const firstError = Object.values(data.errors)
      .flat()
      .find(Boolean);

    if (firstError) return firstError;
  }

  return fallback;
}

async function sendRequest(path, options = {}) {
  const token = sessionStorage.getItem("accessToken");
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...options,
    headers: {
      Authorization: `Bearer ${token}`,
      ...(options.body
        ? { "Content-Type": "application/json" }
        : {}),
      ...options.headers
    }
  });

  const data = await response.json().catch(() => ({}));

  if (response.status === 401) {
    sessionStorage.clear();
    window.location.replace("/");
    throw new AllocationApiError(
      "Your session expired. Please sign in again.",
      401,
      data
    );
  }

  if (response.status === 403) {
    throw new AllocationApiError(
      "You are not authorized to manage allocations.",
      403,
      data
    );
  }

  if (!response.ok) {
    throw new AllocationApiError(
      getErrorMessage(
        data,
        "The allocation request could not be completed."
      ),
      response.status,
      data
    );
  }

  return data;
}

export function getAllocations() {
  return sendRequest("/api/admin/allocations", {
    method: "GET"
  });
}

export function allocateStudent(studentProfileId, roomId) {
  return sendRequest("/api/admin/allocations", {
    method: "POST",
    body: JSON.stringify({
      studentProfileId: Number(studentProfileId),
      roomId: Number(roomId)
    })
  });
}

export function transferStudent(studentProfileId, newRoomId) {
  return sendRequest(
    `/api/admin/allocations/student/${studentProfileId}/transfer`,
    {
      method: "PUT",
      body: JSON.stringify({ newRoomId: Number(newRoomId) })
    }
  );
}

export function getOccupancyReport(filters = {}) {
  const query = new URLSearchParams();

  if (filters.blockId) query.set("blockId", filters.blockId);
  if (filters.floorNumber) {
    query.set("floorNumber", filters.floorNumber);
  }

  const suffix = query.toString();

  return sendRequest(
    `/api/admin/allocations/occupancy${suffix ? `?${suffix}` : ""}`,
    { method: "GET" }
  );
}
