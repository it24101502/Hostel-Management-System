export const MAX_DESCRIPTION_LENGTH = 1000;
export const MAX_REMARKS_LENGTH = 500;

// Must match ComplaintCategories.cs in the backend.
export const complaintCategories = [
  { value: "MAINTENANCE", label: "Maintenance" },
  { value: "ELECTRICAL", label: "Electrical" },
  { value: "PLUMBING", label: "Plumbing" },
  { value: "CLEANLINESS", label: "Cleanliness" },
  { value: "SECURITY", label: "Security" },
  { value: "FOOD", label: "Food" },
  { value: "NOISE", label: "Noise" },
  { value: "OTHER", label: "Other" }
];

export const categoryLabels = Object.fromEntries(
  complaintCategories.map((c) => [c.value, c.label])
);

export const complaintStatusLabels = {
  OPEN: "Open",
  IN_PROGRESS: "In progress",
  RESOLVED: "Resolved"
};