export const MAX_TITLE_LENGTH = 200;
export const MAX_CONTENT_LENGTH = 4000;

export const noticeTypes = [
  { value: "NOTICE", label: "Notice" },
  { value: "SCHEDULE", label: "Schedule" }
];

export const noticeTypeLabels = Object.fromEntries(
  noticeTypes.map((type) => [type.value, type.label])
);

// Display only. The server decides what a student may see.
// Reads the block claim from the token issued at login.
export function getOwnBlockId() {
  try {
    const token = sessionStorage.getItem("accessToken");
    let payload = token.split(".")[1]
      .replace(/-/g, "+")
      .replace(/_/g, "/");
    payload = payload.padEnd(Math.ceil(payload.length / 4) * 4, "=");
    return JSON.parse(atob(payload)).hostel_block_id ?? null;
  } catch {
    return null;
  }
}