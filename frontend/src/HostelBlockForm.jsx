import { useState } from "react";

import {
  createHostelBlock,
  RoomApiError
} from "./roomApi.js";

function HostelBlockForm({ onCancel, onSaved }) {
  const [values, setValues] = useState({
    blockCode: "",
    blockName: ""
  });

  const [errors, setErrors] = useState({});
  const [serverMessage, setServerMessage] =
    useState("");
  const [isSubmitting, setIsSubmitting] =
    useState(false);

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

  function validate() {
    const validationErrors = {};
    const blockCode = values.blockCode.trim();
    const blockName = values.blockName.trim();

    if (!blockCode) {
      validationErrors.blockCode =
        "Block code is required.";
    } else if (blockCode.length > 20) {
      validationErrors.blockCode =
        "Block code cannot exceed 20 characters.";
    }

    if (!blockName) {
      validationErrors.blockName =
        "Block name is required.";
    } else if (blockName.length > 100) {
      validationErrors.blockName =
        "Block name cannot exceed 100 characters.";
    }

    setErrors(validationErrors);

    return Object.keys(validationErrors).length === 0;
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setServerMessage("");

    if (!validate()) {
      return;
    }

    setIsSubmitting(true);

    try {
      const createdBlock =
        await createHostelBlock(values);

      onSaved(
        createdBlock,
        `Block ${createdBlock.blockCode} was created successfully.`
      );
    } catch (error) {
      if (
        error instanceof RoomApiError &&
        error.status !== 401
      ) {
        setServerMessage(error.message);
      } else {
        setServerMessage(
          "Unable to create the block. Please try again."
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
          <p>NEW HOSTEL BLOCK</p>
          <h2>Create a block</h2>
          <span>
            Add a block before creating rooms inside it.
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
        className="admin-user-form"
        onSubmit={handleSubmit}
        noValidate
      >
        <div className="admin-form-grid">
          <div className="form-group">
            <label htmlFor="blockCode">
              Block code
            </label>

            <input
              id="blockCode"
              name="blockCode"
              type="text"
              maxLength="20"
              value={values.blockCode}
              onChange={updateValue}
              aria-invalid={Boolean(errors.blockCode)}
              placeholder="Example: BLOCK-B"
            />

            {errors.blockCode && (
              <p className="field-error">
                {errors.blockCode}
              </p>
            )}
          </div>

          <div className="form-group">
            <label htmlFor="blockName">
              Block name
            </label>

            <input
              id="blockName"
              name="blockName"
              type="text"
              maxLength="100"
              value={values.blockName}
              onChange={updateValue}
              aria-invalid={Boolean(errors.blockName)}
              placeholder="Example: Boys Hostel Block B"
            />

            {errors.blockName && (
              <p className="field-error">
                {errors.blockName}
              </p>
            )}
          </div>
        </div>

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
              ? "Creating..."
              : "Create block"}
          </button>
        </div>
      </form>
    </section>
  );
}

export default HostelBlockForm;