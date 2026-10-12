import { useEffect, useState } from "react";

import {
  ComplaintApiError,
  getStaffDirectory
} from "./complaintApi.js";

import {
  categoryLabels,
  complaintStatusLabels,
  MAX_REMARKS_LENGTH
} from "./complaintFormat.js";

/* ---------- Assign ---------- */

export function ComplaintAssignDialog({
  complaint,
  onCancel,
  onSubmit
}) {
  const [assigneeId, setAssigneeId] = useState("");
  const [error, setError] = useState("");
  const [serverMessage, setServerMessage] =
    useState("");
  const [isSubmitting, setIsSubmitting] =
    useState(false);
  const [staff, setStaff] = useState([]);
  const [staffFailed, setStaffFailed] =
    useState(false);

  useEffect(() => {
    let cancelled = false;

    getStaffDirectory()
      .then((list) => {
        if (!cancelled) setStaff(list);
      })
      .catch(() => {
        if (!cancelled) setStaffFailed(true);
      });

    return () => {
      cancelled = true;
    };
  }, []);

  async function handleSubmit(event) {
    event.preventDefault();
    setServerMessage("");

    const trimmed = assigneeId.trim();

    setIsSubmitting(true);

    try {
      await onSubmit(trimmed ? Number(trimmed) : null);
    } catch (submitError) {
      if (
        submitError instanceof ComplaintApiError &&
        submitError.status !== 401
      ) {
        setServerMessage(submitError.message);
      } else {
        setServerMessage(
          "Unable to assign the complaint. Please try again."
        );
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div
      className="admin-dialog-backdrop"
      role="presentation"
      onMouseDown={(event) => {
        if (
          event.target === event.currentTarget &&
          !isSubmitting
        ) {
          onCancel();
        }
      }}
    >
      <section
        className="admin-confirm-dialog leave-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="complaint-assign-title"
      >
        <div className="leave-dialog-icon">→</div>

        <h2 id="complaint-assign-title">
          Assign complaint #{complaint.complaintId}
        </h2>

        <p>
          <strong>{complaint.studentUsername}</strong> ·{" "}
          {categoryLabels[complaint.category] ??
            complaint.category}
        </p>

        {serverMessage && (
          <div className="message error" role="alert">
            {serverMessage}
          </div>
        )}

        <form
          className="leave-dialog-form complaint-dialog-form"
          onSubmit={handleSubmit}
          noValidate
        >
          <div className="form-group">
            <label htmlFor="complaintAssignee">
              Assign to
            </label>

            <select
              id="complaintAssignee"
              value={assigneeId}
              onChange={(event) => {
                setAssigneeId(event.target.value);
                setError("");
              }}
              aria-invalid={Boolean(error)}
            >
              <option value="">Myself</option>
              {staff.map((member) => (
                <option
                  key={member.userId}
                  value={member.userId}
                >
                  {member.fullName || member.username}
                  {" · "}
                  {member.roleName}
                </option>
              ))}
            </select>

            {staffFailed && (
              <p className="field-error">
                The staff list could not be loaded. You
                can still assign the complaint to
                yourself.
              </p>
            )}

            {error && (
              <p className="field-error">{error}</p>
            )}
          </div>

          <div className="admin-dialog-actions">
            <button
              type="button"
              className="secondary-button"
              disabled={isSubmitting}
              onClick={onCancel}
            >
              Cancel
            </button>

            <button
              type="submit"
              className="primary-button"
              disabled={isSubmitting}
            >
              {isSubmitting
                ? "Saving..."
                : assigneeId.trim()
                  ? "Assign"
                  : "Assign to me"}
            </button>
          </div>
        </form>
      </section>
    </div>
  );
}

/* ---------- Change status ---------- */

export function ComplaintStatusDialog({
  complaint,
  onCancel,
  onSubmit
}) {
  // The backend refuses a change to the same status.
  const options = Object.keys(complaintStatusLabels).filter(
    (status) => status !== complaint.status
  );

  const [status, setStatus] = useState(options[0]);
  const [remarks, setRemarks] = useState("");
  const [error, setError] = useState("");
  const [serverMessage, setServerMessage] =
    useState("");
  const [isSubmitting, setIsSubmitting] =
    useState(false);

  async function handleSubmit(event) {
    event.preventDefault();
    setServerMessage("");

    if (remarks.trim().length > MAX_REMARKS_LENGTH) {
      setError(
        `Remarks cannot exceed ${MAX_REMARKS_LENGTH} characters.`
      );
      return;
    }

    setIsSubmitting(true);

    try {
      await onSubmit(status, remarks);
    } catch (submitError) {
      if (
        submitError instanceof ComplaintApiError &&
        submitError.status !== 401
      ) {
        setServerMessage(submitError.message);
      } else {
        setServerMessage(
          "Unable to update the status. Please try again."
        );
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div
      className="admin-dialog-backdrop"
      role="presentation"
      onMouseDown={(event) => {
        if (
          event.target === event.currentTarget &&
          !isSubmitting
        ) {
          onCancel();
        }
      }}
    >
      <section
        className="admin-confirm-dialog leave-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="complaint-status-title"
      >
        <div className="leave-dialog-icon approve">✓</div>

        <h2 id="complaint-status-title">
          Update complaint #{complaint.complaintId}
        </h2>

        <p>
          Currently{" "}
          <strong>
            {complaintStatusLabels[complaint.status]}
          </strong>
          . The student is notified when you save.
        </p>

        {serverMessage && (
          <div className="message error" role="alert">
            {serverMessage}
          </div>
        )}

        <form
          className="leave-dialog-form complaint-dialog-form"
          onSubmit={handleSubmit}
          noValidate
        >
          <div className="form-group">
            <label htmlFor="complaintNewStatus">
              New status
            </label>

            <select
              id="complaintNewStatus"
              value={status}
              onChange={(event) =>
                setStatus(event.target.value)
              }
            >
              {options.map((option) => (
                <option key={option} value={option}>
                  {complaintStatusLabels[option]}
                </option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label htmlFor="complaintRemarks">
              Remarks (optional)
            </label>

            <textarea
              id="complaintRemarks"
              rows="3"
              maxLength={MAX_REMARKS_LENGTH}
              value={remarks}
              onChange={(event) => {
                setRemarks(event.target.value);
                setError("");
              }}
              aria-invalid={Boolean(error)}
              placeholder="Example: Plumber visited, tap replaced"
            />

            <p className="leave-char-count">
              {remarks.length}/{MAX_REMARKS_LENGTH}
            </p>

            {error && (
              <p className="field-error">{error}</p>
            )}
          </div>

          <div className="admin-dialog-actions">
            <button
              type="button"
              className="secondary-button"
              disabled={isSubmitting}
              onClick={onCancel}
            >
              Cancel
            </button>

            <button
              type="submit"
              className="primary-button"
              disabled={isSubmitting}
            >
              {isSubmitting ? "Saving..." : "Save status"}
            </button>
          </div>
        </form>
      </section>
    </div>
  );
}