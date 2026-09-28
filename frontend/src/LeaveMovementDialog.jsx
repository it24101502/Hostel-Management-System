import { useState } from "react";

import { LeaveApiError } from "./leaveReviewApi.js";
import { formatDate } from "./leaveFormat.js";

// Confirms recording a student's departure or return.
// `onConfirm()` must return a promise that rejects on failure.
function LeaveMovementDialog({
  request,
  kind,
  onCancel,
  onConfirm
}) {
  const isDeparture = kind === "departure";

  const [serverMessage, setServerMessage] =
    useState("");
  const [isSubmitting, setIsSubmitting] =
    useState(false);

  async function handleConfirm() {
    setServerMessage("");
    setIsSubmitting(true);

    try {
      await onConfirm();
    } catch (error) {
      if (
        error instanceof LeaveApiError &&
        error.status !== 401
      ) {
        setServerMessage(error.message);
      } else {
        setServerMessage(
          "Unable to record this. Please try again."
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
        role="alertdialog"
        aria-modal="true"
        aria-labelledby="leave-movement-title"
      >
        <div className="leave-dialog-icon">
          {isDeparture ? "→" : "←"}
        </div>

        <h2 id="leave-movement-title">
          {isDeparture
            ? "Record departure?"
            : "Record return?"}
        </h2>

        <p>
          <strong>{request.studentUsername}</strong>{" "}
          {isDeparture
            ? "is leaving the hostel now."
            : "has returned to the hostel now."}{" "}
          The time will be recorded as the current time.
          Expected return date:{" "}
          {formatDate(request.expectedReturnDate)}.
        </p>

        {serverMessage && (
          <div className="message error" role="alert">
            {serverMessage}
          </div>
        )}

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
            type="button"
            className="primary-button"
            disabled={isSubmitting}
            onClick={handleConfirm}
          >
            {isSubmitting
              ? "Saving..."
              : isDeparture
                ? "Record departure"
                : "Record return"}
          </button>
        </div>
      </section>
    </div>
  );
}

export default LeaveMovementDialog;
