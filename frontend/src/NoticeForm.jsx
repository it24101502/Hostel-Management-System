import { useState } from "react";

import {
  createNotice,
  NoticeApiError,
  updateNotice
} from "./noticeApi.js";

import {
  MAX_CONTENT_LENGTH,
  MAX_TITLE_LENGTH,
  noticeTypes
} from "./noticeFormat.js";

import { toDateInputValue } from "./leaveFormat.js";

function getInitialValues(notice) {
  return {
    title: notice?.title ?? "",
    noticeType: notice?.noticeType ?? "NOTICE",
    hostelBlockId: notice?.hostelBlockId?.toString() ?? "",
    expiryDate: notice?.expiryDate ?? "",
    content: notice?.content ?? ""
  };
}

function NoticeForm({ notice, blocks, blocksFailed, onCancel, onSaved }) {
  const isEditing = Boolean(notice);
  const today = toDateInputValue(new Date());

  const [values, setValues] = useState(getInitialValues(notice));
  const [errors, setErrors] = useState({});
  const [serverMessage, setServerMessage] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  function validate() {
    const found = {};
    const title = values.title.trim();
    const content = values.content.trim();

    if (!title) found.title = "Title is required.";
    else if (title.length > MAX_TITLE_LENGTH)
      found.title = `Title cannot exceed ${MAX_TITLE_LENGTH} characters.`;

    if (!content) found.content = "Content is required.";
    else if (content.length > MAX_CONTENT_LENGTH)
      found.content = `Content cannot exceed ${MAX_CONTENT_LENGTH} characters.`;

    if (!values.expiryDate) found.expiryDate = "Expiry date is required.";
    else if (values.expiryDate < today)
      found.expiryDate = "Expiry date cannot be in the past.";

    setErrors(found);
    return Object.keys(found).length === 0;
  }

  function updateValue(event) {
    const { name, value } = event.target;
    setValues((current) => ({ ...current, [name]: value }));

    if (errors[name]) {
      setErrors((current) => ({ ...current, [name]: "" }));
    }
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setServerMessage("");

    if (!validate()) return;

    setIsSubmitting(true);

    try {
      const saved = isEditing
        ? await updateNotice(notice.noticeId, values)
        : await createNotice(values);

      onSaved(
        saved,
        isEditing
          ? "The notice was updated."
          : "The notice was published."
      );
    } catch (error) {
      if (error instanceof NoticeApiError && error.status !== 401) {
        setServerMessage(error.message);

        // Server validation errors, matched ignoring key case.
        const serverErrors = {};
        Object.entries(error.details?.errors ?? {}).forEach(([key, msgs]) => {
          serverErrors[key.toLowerCase()] = msgs;
        });

        const fieldErrors = {};
        Object.keys(values).forEach((field) => {
          const messages = serverErrors[field.toLowerCase()];
          if (Array.isArray(messages) && messages[0]) {
            fieldErrors[field] = messages[0];
          }
        });
        setErrors(fieldErrors);
      } else {
        setServerMessage("Unable to save the notice. Please try again.");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section className="admin-form-panel">
      <div className="admin-panel-heading">
        <div>
          <p>{isEditing ? "EDIT NOTICE" : "NEW NOTICE"}</p>
          <h2>{isEditing ? "Update notice" : "Publish a notice or schedule"}</h2>
          <span>
            Students in the chosen block (or every block) see it until
            the expiry date.
          </span>
        </div>

        <button type="button" className="secondary-button" onClick={onCancel}>
          Close
        </button>
      </div>

      {serverMessage && (
        <div className="message error" role="alert">{serverMessage}</div>
      )}

      <form className="admin-user-form notice-form" onSubmit={handleSubmit} noValidate>
        <div className="admin-form-grid">
          <div className="form-group leave-form-full">
            <label htmlFor="noticeTitle">Title</label>
            <input
              id="noticeTitle"
              name="title"
              type="text"
              maxLength={MAX_TITLE_LENGTH}
              value={values.title}
              onChange={updateValue}
              aria-invalid={Boolean(errors.title)}
              placeholder="Example: Water supply maintenance"
            />
            {errors.title && <p className="field-error">{errors.title}</p>}
          </div>

          <div className="form-group">
            <label htmlFor="noticeType">Type</label>
            <select
              id="noticeType"
              name="noticeType"
              value={values.noticeType}
              onChange={updateValue}
            >
              {noticeTypes.map((type) => (
                <option key={type.value} value={type.value}>{type.label}</option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label htmlFor="noticeBlock">Audience</label>
            <select
              id="noticeBlock"
              name="hostelBlockId"
              value={values.hostelBlockId}
              onChange={updateValue}
            >
              <option value="">All blocks (general)</option>
              {blocks.map((block) => (
                <option key={block.blockId} value={block.blockId}>
                  {block.blockCode} — {block.blockName}
                </option>
              ))}
            </select>
            {blocksFailed && (
              <p className="field-error">
                Hostel blocks could not be loaded. Only general notices
                can be published right now.
              </p>
            )}
          </div>

          <div className="form-group">
            <label htmlFor="noticeExpiry">Expiry date</label>
            <input
              id="noticeExpiry"
              name="expiryDate"
              type="date"
              min={today}
              value={values.expiryDate}
              onChange={updateValue}
              aria-invalid={Boolean(errors.expiryDate)}
            />
            {errors.expiryDate && (
              <p className="field-error">{errors.expiryDate}</p>
            )}
          </div>

          <div className="form-group leave-form-full">
            <label htmlFor="noticeContent">Content</label>
            <textarea
              id="noticeContent"
              name="content"
              rows="6"
              maxLength={MAX_CONTENT_LENGTH}
              value={values.content}
              onChange={updateValue}
              aria-invalid={Boolean(errors.content)}
            />
            <p className="leave-char-count">
              {values.content.length}/{MAX_CONTENT_LENGTH}
            </p>
            {errors.content && <p className="field-error">{errors.content}</p>}
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

          <button type="submit" className="primary-button" disabled={isSubmitting}>
            {isSubmitting ? "Saving..." : isEditing ? "Save changes" : "Publish"}
          </button>
        </div>
      </form>
    </section>
  );
}

export default NoticeForm;