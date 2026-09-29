import { useState } from "react";

import {
  LeaveApiError,
  submitLeaveRequest
} from "./leaveApi.js";

import { toDateInputValue } from "./leaveFormat.js";

const MAX_REASON_LENGTH = 500;
const MAX_COMPANION_NAME_LENGTH = 200;
const MAX_COMPANION_RELATIONSHIP_LENGTH = 100;
const MAX_COMPANION_PHONE_LENGTH = 20;

const emptyValues = {
  departureDate: "",
  expectedReturnDate: "",
  reason: "",
  companionName: "",
  companionRelationship: "",
  companionPhone: ""
};

// Mirrors the server rule: digits, spaces, +, -, brackets,
// with 7 to 15 digits.
function isValidPhone(value) {
  const digitCount = (value.match(/\d/g) ?? []).length;

  return (
    /^[0-9+()\-\s]+$/.test(value) &&
    digitCount >= 7 &&
    digitCount <= 15
  );
}

function LeaveRequestForm({ onCancel, onSaved }) {
  const [values, setValues] = useState(emptyValues);
  const [errors, setErrors] = useState({});
  const [serverMessage, setServerMessage] =
    useState("");
  const [isSubmitting, setIsSubmitting] =
    useState(false);

  const today = toDateInputValue(new Date());

  function validate() {
    const validationErrors = {};

    if (!values.departureDate) {
      validationErrors.departureDate =
        "Departure date is required.";
    } else if (values.departureDate < today) {
      validationErrors.departureDate =
        "Departure date cannot be in the past.";
    }

    if (!values.expectedReturnDate) {
      validationErrors.expectedReturnDate =
        "Expected return date is required.";
    } else if (
      values.departureDate &&
      values.expectedReturnDate < values.departureDate
    ) {
      validationErrors.expectedReturnDate =
        "Expected return date cannot be earlier than the departure date.";
    }

    const reason = values.reason.trim();

    if (!reason) {
      validationErrors.reason = "Reason is required.";
    } else if (reason.length > MAX_REASON_LENGTH) {
      validationErrors.reason =
        `Reason cannot exceed ${MAX_REASON_LENGTH} characters.`;
    }

    const companionName = values.companionName.trim();

    if (!companionName) {
      validationErrors.companionName =
        "Companion or guardian name is required.";
    } else if (
      companionName.length > MAX_COMPANION_NAME_LENGTH
    ) {
      validationErrors.companionName =
        `Name cannot exceed ${MAX_COMPANION_NAME_LENGTH} characters.`;
    }

    const relationship =
      values.companionRelationship.trim();

    if (!relationship) {
      validationErrors.companionRelationship =
        "Relationship to the student is required.";
    } else if (
      relationship.length >
      MAX_COMPANION_RELATIONSHIP_LENGTH
    ) {
      validationErrors.companionRelationship =
        `Relationship cannot exceed ${MAX_COMPANION_RELATIONSHIP_LENGTH} characters.`;
    }

    const phone = values.companionPhone.trim();

    if (!phone) {
      validationErrors.companionPhone =
        "Companion or guardian phone number is required.";
    } else if (
      phone.length > MAX_COMPANION_PHONE_LENGTH ||
      !isValidPhone(phone)
    ) {
      validationErrors.companionPhone =
        "Enter a valid phone number using digits, spaces, +, - and brackets.";
    }

    setErrors(validationErrors);

    return Object.keys(validationErrors).length === 0;
  }

  function updateValue(event) {
    const { name, value } = event.target;

    setValues((current) => ({
      ...current,
      [name]: value
    }));

    if (errors[name]) {
      setErrors((current) => ({
        ...current,
        [name]: ""
      }));
    }
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setServerMessage("");

    if (!validate()) {
      return;
    }

    setIsSubmitting(true);

    try {
      const savedRequest =
        await submitLeaveRequest(values);

      onSaved(
        savedRequest,
        "Your leave request was submitted and is waiting for approval."
      );
    } catch (error) {
      if (
        error instanceof LeaveApiError &&
        error.status !== 401
      ) {
        setServerMessage(error.message);

        // The server reports problems per field as
        // { errors: { fieldName: ["message"] } }.
        const serverErrors =
          error.details?.errors ?? {};
        const fieldErrors = {};

        Object.keys(emptyValues).forEach((fieldName) => {
          const messages = serverErrors[fieldName];

          if (Array.isArray(messages) && messages[0]) {
            fieldErrors[fieldName] = messages[0];
          }
        });

        setErrors(fieldErrors);
      } else {
        setServerMessage(
          "Unable to submit the leave request. Please try again."
        );
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section className="admin-form-panel">
      <div className="admin-panel-heading">
        <div>
          <p>NEW LEAVE REQUEST</p>
          <h2>Request leave</h2>
          <span>
            Enter your travel dates, the reason and the
            person you will be travelling with.
          </span>
        </div>

        <button
          type="button"
          className="secondary-button"
          onClick={onCancel}
        >
          Close
        </button>
      </div>

      {serverMessage && (
        <div className="message error" role="alert">
          {serverMessage}
        </div>
      )}

      <form
        className="admin-user-form leave-form"
        onSubmit={handleSubmit}
        noValidate
      >
        <div className="admin-form-grid">
          <div className="form-group">
            <label htmlFor="leaveDepartureDate">
              Departure date
            </label>
            <input
              id="leaveDepartureDate"
              name="departureDate"
              type="date"
              min={today}
              value={values.departureDate}
              onChange={updateValue}
              aria-invalid={Boolean(errors.departureDate)}
            />
            {errors.departureDate && (
              <p className="field-error">
                {errors.departureDate}
              </p>
            )}
          </div>

          <div className="form-group">
            <label htmlFor="leaveExpectedReturnDate">
              Expected return date
            </label>
            <input
              id="leaveExpectedReturnDate"
              name="expectedReturnDate"
              type="date"
              min={values.departureDate || today}
              value={values.expectedReturnDate}
              onChange={updateValue}
              aria-invalid={
                Boolean(errors.expectedReturnDate)
              }
            />
            {errors.expectedReturnDate && (
              <p className="field-error">
                {errors.expectedReturnDate}
              </p>
            )}
          </div>

          <div className="form-group leave-form-full">
            <label htmlFor="leaveReason">
              Reason for leave
            </label>
            <textarea
              id="leaveReason"
              name="reason"
              rows="4"
              maxLength={MAX_REASON_LENGTH}
              value={values.reason}
              onChange={updateValue}
              aria-invalid={Boolean(errors.reason)}
              placeholder="Example: Attending a family wedding"
            />
            <p className="leave-char-count">
              {values.reason.length}/{MAX_REASON_LENGTH}
            </p>
            {errors.reason && (
              <p className="field-error">
                {errors.reason}
              </p>
            )}
          </div>

          <div className="form-group">
            <label htmlFor="leaveCompanionName">
              Companion or guardian name
            </label>
            <input
              id="leaveCompanionName"
              name="companionName"
              type="text"
              maxLength={MAX_COMPANION_NAME_LENGTH}
              value={values.companionName}
              onChange={updateValue}
              aria-invalid={Boolean(errors.companionName)}
              placeholder="Full name"
            />
            {errors.companionName && (
              <p className="field-error">
                {errors.companionName}
              </p>
            )}
          </div>

          <div className="form-group">
            <label htmlFor="leaveCompanionRelationship">
              Relationship to you
            </label>
            <input
              id="leaveCompanionRelationship"
              name="companionRelationship"
              type="text"
              maxLength={MAX_COMPANION_RELATIONSHIP_LENGTH}
              value={values.companionRelationship}
              onChange={updateValue}
              aria-invalid={
                Boolean(errors.companionRelationship)
              }
              placeholder="Example: Father"
            />
            {errors.companionRelationship && (
              <p className="field-error">
                {errors.companionRelationship}
              </p>
            )}
          </div>

          <div className="form-group">
            <label htmlFor="leaveCompanionPhone">
              Companion or guardian phone
            </label>
            <input
              id="leaveCompanionPhone"
              name="companionPhone"
              type="tel"
              maxLength={MAX_COMPANION_PHONE_LENGTH}
              value={values.companionPhone}
              onChange={updateValue}
              aria-invalid={Boolean(errors.companionPhone)}
              placeholder="Example: 0771234567"
            />
            {errors.companionPhone && (
              <p className="field-error">
                {errors.companionPhone}
              </p>
            )}
          </div>
        </div>

        <p className="leave-form-help">
          Your request is sent to the wardens for review.
          You can follow its status on this page.
        </p>

        <div className="admin-form-actions">
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
              ? "Submitting..."
              : "Submit request"}
          </button>
        </div>
      </form>
    </section>
  );
}

export default LeaveRequestForm;
