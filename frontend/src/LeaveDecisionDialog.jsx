import { useState } from "react";

import { LeaveApiError } from "./leaveReviewApi.js";
import { formatDate } from "./leaveFormat.js";

const MAX_REASON_LENGTH = 500;

// Asks the warden for the reason behind an approval or rejection.
// `onSubmit(reason)` must return a promise that rejects on failure.
function LeaveDecisionDialog({
  request,
  decision,
  onCancel,
  onSubmit
}) {
  const isApproval = decision === "APPROVE";

  const [reason, setReason] = useState("");
  const [error, setError] = useState("");
  const [serverMessage, setServerMessage] =
    useState("");
  const [isSubmitting, setIsSubmitting] =
    useState(false);

  async function handleSubmit(event) {
    event.preventDefault();
    setServerMessage("");

    const trimmedReason = reason.trim();

    if (!trimmedReason) {
      setError("A reason for the decision is required.");
      return;
    }

    if (trimmedReason.length > MAX_REASON_LENGTH) {
      setError(
        `Reason cannot exceed ${MAX_REASON_LENGTH} characters.`
      );
      return;
    }

    setIsSubmitting(true);

    try {
      await onSubmit(trimmedReason);
    } catch (submitError) {
      if (
        submitError instanceof LeaveApiError &&
        submitError.status !== 401
      ) {
        setServerMessage(submitError.message);
      } else {
        setServerMessage(
          "Unable to save the decision. Please try again."
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
        aria-labelledby="leave-decision-title"
      >
        <div
          className={
            isApproval
              ? "leave-dialog-icon approve"
              : "admin-warning-icon"
          }
        >
          {isApproval ? "✓" : "!"}
        </div>

        <h2 id="leave-decision-title">
          {isApproval
            ? "Approve leave request?"
            : "Reject leave request?"}
        </h2>

        <p>
          <strong>{request.studentUsername}</strong>{" "}
          · {formatDate(request.departureDate)} to{" "}
          {formatDate(request.expectedReturnDate)}
        </p>

        {serverMessage && (
          <div className="message error" role="alert">
            {serverMessage}
          </div>
        )}

        <form
          className="leave-dialog-form"
          onSubmit={handleSubmit}
          noValidate
        >
          <div className="form-group">
            <label htmlFor="leaveDecisionReason">
              Reason for this decision
            </label>

            <textarea
              id="leaveDecisionReason"
              rows="4"
              maxLength={MAX_REASON_LENGTH}
              value={reason}
              onChange={(event) => {
                setReason(event.target.value);
                setError("");
              }}
              aria-invalid={Boolean(error)}
              placeholder={
                isApproval
                  ? "Example: Guardian confirmed by phone"
                  : "Example: Examinations start that week"
              }
            />

            <p className="leave-char-count">
              {reason.length}/{MAX_REASON_LENGTH}
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
              className={
                isApproval
                  ? "primary-button"
                  : "danger-button"
              }
              disabled={isSubmitting}
            >
              {isSubmitting
                ? "Saving..."
                : isApproval
                  ? "Approve request"
                  : "Reject request"}
            </button>
          </div>
        </form>
      </section>
    </div>
  );
}

export default LeaveDecisionDialog;
