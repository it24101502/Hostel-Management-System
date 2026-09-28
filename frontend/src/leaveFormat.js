// Small display helpers shared by the leave pages.

// Turns a "YYYY-MM-DD" date from the API into "23 Sep 2026".
// The parts are read manually so the browser's time zone can
// never shift the date by a day.
export function formatDate(value) {
  if (!value) {
    return "—";
  }

  const [year, month, day] = value
    .split("-")
    .map(Number);

  return new Intl.DateTimeFormat("en-GB", {
    year: "numeric",
    month: "short",
    day: "numeric"
  }).format(new Date(year, month - 1, day));
}

// Turns a UTC timestamp from the API into the viewer's local time.
export function formatDateTime(value) {
  if (!value) {
    return "—";
  }

  return new Intl.DateTimeFormat("en-GB", {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit"
  }).format(new Date(value));
}

// Formats a Date as "YYYY-MM-DD" for <input type="date">
// using the viewer's local calendar date.
export function toDateInputValue(date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");

  return `${year}-${month}-${day}`;
}

export const statusLabels = {
  PENDING: "Pending",
  APPROVED: "Approved",
  REJECTED: "Rejected",
  DEPARTED: "Departed",
  CLOSED: "Closed"
};
