import { useState } from "react";

import {
  ComplaintApiError,
  submitComplaint
} from "./complaintApi.js";

import {
  complaintCategories,
  MAX_DESCRIPTION_LENGTH
} from "./complaintFormat.js";

const emptyValues = {
  category: "",
  description: ""
};

function ComplaintForm({ onCancel, onSaved }) {
  const [values, setValues] = useState(emptyValues);
  const [errors, setErrors] = useState({});
  const [serverMessage, setServerMessage] =
    useState("");
  const [isSubmitting, setIsSubmitting] =
    useState(false);

  function validate() {
    const validationErrors = {};

    if (!values.category) {
      validationErrors.category =
        "Select a category.";
    }

    const description = values.description.trim();

    if (!description) {
      validationErrors.description =
        "Description is required.";
    } else if (description.length > MAX_DESCRIPTION_LENGTH) {
      validationErrors.description =
        `Description cannot exceed ${MAX_DESCRIPTION_LENGTH} characters.`;
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
      const saved = await submitComplaint(values);

      onSaved(
        saved,
        "Your complaint was submitted. You will be notified when its status changes."
      );
    } catch (error) {
      if (
        error instanceof ComplaintApiError &&
        error.status !== 401
      ) {
        setServerMessage(error.message);

        const serverErrors = error.details?.errors ?? {};
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
          "Unable to submit the complaint. Please try again."
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
          <p>NEW COMPLAINT</p>
          <h2>Submit a complaint</h2>
          <span>
            Choose a category and describe the problem.
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
        className="admin-user-form complaint-form"
        onSubmit={handleSubmit}
        noValidate
      >
        <div className="admin-form-grid">
          <div className="form-group leave-form-full">
            <label htmlFor="complaintCategory">
              Category
            </label>

            <select
              id="complaintCategory"
              name="category"
              value={values.category}
              onChange={updateValue}
              aria-invalid={Boolean(errors.category)}
            >
              <option value="">Select a category</option>

              {complaintCategories.map((category) => (
                <option
                  key={category.value}
                  value={category.value}
                >
                  {category.label}
                </option>
              ))}
            </select>

            {errors.category && (
              <p className="field-error">
                {errors.category}
              </p>
            )}
          </div>

          <div className="form-group leave-form-full">
            <label htmlFor="complaintDescription">
              Description
            </label>

            <textarea
              id="complaintDescription"
              name="description"
              rows="5"
              maxLength={MAX_DESCRIPTION_LENGTH}
              value={values.description}
              onChange={updateValue}
              aria-invalid={Boolean(errors.description)}
              placeholder="Example: The tap in room 101 has been leaking since Monday"
            />

            <p className="leave-char-count">
              {values.description.length}/{MAX_DESCRIPTION_LENGTH}
            </p>

            {errors.description && (
              <p className="field-error">
                {errors.description}
              </p>
            )}
          </div>
        </div>

        <p className="leave-form-help">
          Your complaint is sent to the hostel staff. You can
          follow its progress on this page.
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
              : "Submit complaint"}
          </button>
        </div>
      </form>
    </section>
  );
}

export default ComplaintForm;