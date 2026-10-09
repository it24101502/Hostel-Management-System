import { getActiveBlocks } from "./roomApi.js";

export { getActiveBlocks };

const API_BASE_URL =
  import.meta.env.VITE_NOTICE_API_BASE_URL ??
  "http://localhost:8084";

export class NoticeApiError extends Error {
  constructor(message, status, details = {}) {
    super(message);
    this.name = "NoticeApiError";
    this.status = status;
    this.details = details;
  }
}

function getErrorMessage(data, fallback) {
  if (data.message) return data.message;

  if (data.errors) {
    const first = Object.values(data.errors).flat().find(Boolean);
    if (first) return first;
  }

  return fallback;
}

async function sendNoticeRequest(
  path,
  options = {},
  forbiddenMessage = "You are not authorized to use notices."
) {
  const accessToken = sessionStorage.getItem("accessToken");

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...options,
    headers: {
      Authorization: `Bearer ${accessToken}`,
      ...(options.body ? { "Content-Type": "application/json" } : {}),
      ...options.headers
    }
  });

  const data =
    response.status === 204
      ? {}
      : await response.json().catch(() => ({}));

  if (response.status === 401) {
    sessionStorage.clear();
    window.location.replace("/");
    throw new NoticeApiError(
      "Your session expired. Please sign in again.", 401, data);
  }

  if (response.status === 403) {
    throw new NoticeApiError(forbiddenMessage, 403, data);
  }

  if (!response.ok) {
    throw new NoticeApiError(
      getErrorMessage(data, "The notice request could not be completed."),
      response.status,
      data
    );
  }

  return data;
}

const staffMessage = "You are not authorized to manage notices.";

function toPayload(notice) {
  return {
    title: notice.title.trim(),
    content: notice.content.trim(),
    noticeType: notice.noticeType,
    hostelBlockId: notice.hostelBlockId
      ? Number(notice.hostelBlockId)
      : null,
    expiryDate: notice.expiryDate
  };
}

/* Staff (HMS-63) */

export function getNotices(includeArchived = false) {
  return sendNoticeRequest(
    `/api/notices?includeArchived=${includeArchived}`,
    { method: "GET" },
    staffMessage
  );
}

export function createNotice(notice) {
  return sendNoticeRequest(
    "/api/notices",
    { method: "POST", body: JSON.stringify(toPayload(notice)) },
    staffMessage
  );
}

export function updateNotice(noticeId, notice) {
  return sendNoticeRequest(
    `/api/notices/${noticeId}`,
    { method: "PUT", body: JSON.stringify(toPayload(notice)) },
    staffMessage
  );
}

export function deleteNotice(noticeId) {
  return sendNoticeRequest(
    `/api/notices/${noticeId}`,
    { method: "DELETE" },
    staffMessage
  );
}

/* Student (HMS-64) */

export function getMyNotices() {
  return sendNoticeRequest(
    "/api/notices/my",
    { method: "GET" },
    "You are not authorized to view notices."
  );
}