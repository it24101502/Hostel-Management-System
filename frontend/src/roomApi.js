const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ??
  "http://localhost:5220";

export class RoomApiError extends Error {
  constructor(message, status, details = {}) {
    super(message);

    this.name = "RoomApiError";
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

async function sendRoomRequest(path, options = {}) {
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

    throw new RoomApiError(
      "Your session expired. Please sign in again.",
      401,
      data
    );
  }

  if (response.status === 403) {
    throw new RoomApiError(
      "You are not authorized to manage rooms.",
      403,
      data
    );
  }

  if (!response.ok) {
    throw new RoomApiError(
      getErrorMessage(
        data,
        "The room request could not be completed."
      ),
      response.status,
      data
    );
  }

  return data;
}

export function getRooms() {
  return sendRoomRequest("/api/admin/rooms", {
    method: "GET"
  });
}

export function createRoom(room) {
  return sendRoomRequest("/api/admin/rooms", {
    method: "POST",
    body: JSON.stringify({
      blockId: Number(room.blockId),
      floorNumber: Number(room.floorNumber),
      roomNumber: room.roomNumber.trim(),
      bedCapacity: Number(room.bedCapacity)
    })
  });
}

export function updateRoom(roomId, room) {
  return sendRoomRequest(
    `/api/admin/rooms/${roomId}`,
    {
      method: "PUT",
      body: JSON.stringify({
        blockId: Number(room.blockId),
        floorNumber: Number(room.floorNumber),
        roomNumber: room.roomNumber.trim(),
        bedCapacity: Number(room.bedCapacity)
      })
    }
  );
}

export function deleteRoom(roomId) {
  return sendRoomRequest(
    `/api/admin/rooms/${roomId}`,
    {
      method: "DELETE"
    }
  );
}
