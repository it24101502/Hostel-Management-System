import AppShell from "./AppShell.jsx";

import { useEffect, useState } from "react";

import {
  ToolsApiError,
  createContact,
  getContacts,
  getOwnProfile,
  getStudentProfiles,
  updateContact
} from "./adminToolsApi.js";

const CONTACT_TYPES = [
  { value: "GUARDIAN", label: "Guardian" },
  { value: "EMERGENCY", label: "Emergency contact" }
];

const EMPTY_FORM = {
  contactType: "GUARDIAN",
  fullName: "",
  relationship: "",
  phoneNumber: "",
  alternatePhone: "",
  email: "",
  address: "",
  isPrimary: false,
  isActive: true
};

const PHONE_PATTERN = /^[0-9+\-\s()]{7,20}$/;

function errorText(error, fallback) {
  return error instanceof ToolsApiError &&
    error.status !== 401
    ? error.message
    : fallback;
}

/* ---------- Add / edit dialog ---------- */

function ContactDialog({
  contact,
  studentProfileId,
  onCancel,
  onSaved
}) {
  const isEditing = Boolean(contact);

  const [values, setValues] = useState(
    contact
      ? {
          contactType: contact.contactType,
          fullName: contact.fullName,
          relationship: contact.relationship,
          phoneNumber: contact.phoneNumber,
          alternatePhone: contact.alternatePhone ?? "",
          email: contact.email ?? "",
          address: contact.address ?? "",
          isPrimary: contact.isPrimary,
          isActive: contact.isActive
        }
      : EMPTY_FORM
  );
  const [errors, setErrors] = useState({});
  const [serverMessage, setServerMessage] = useState("");
  const [isSaving, setIsSaving] = useState(false);

  function handleChange(event) {
    const { name, value, type, checked } = event.target;

    setValues((current) => ({
      ...current,
      [name]: type === "checkbox" ? checked : value
    }));
    setErrors((current) => ({ ...current, [name]: "" }));
    setServerMessage("");
  }

  function validate() {
    const found = {};

    if (!values.fullName.trim()) {
      found.fullName = "Full name is required.";
    }

    if (!values.relationship.trim()) {
      found.relationship = "Relationship is required.";
    }

    if (!PHONE_PATTERN.test(values.phoneNumber.trim())) {
      found.phoneNumber = "Enter a valid phone number.";
    }

    if (
      values.alternatePhone.trim() &&
      !PHONE_PATTERN.test(values.alternatePhone.trim())
    ) {
      found.alternatePhone = "Enter a valid phone number.";
    }

    if (
      values.email.trim() &&
      !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(values.email.trim())
    ) {
      found.email = "Enter a valid email address.";
    }

    setErrors(found);

    return Object.keys(found).length === 0;
  }

  async function handleSubmit(event) {
    event.preventDefault();

    if (!validate()) {
      return;
    }

    const body = {
      contactType: values.contactType,
      fullName: values.fullName.trim(),
      relationship: values.relationship.trim(),
      phoneNumber: values.phoneNumber.trim(),
      alternatePhone: values.alternatePhone.trim() || null,
      email: values.email.trim() || null,
      address: values.address.trim() || null,
      isPrimary: values.isPrimary
    };

    setIsSaving(true);

    try {
      const saved = isEditing
        ? await updateContact(
            studentProfileId,
            contact.guardianContactId,
            { ...body, isActive: values.isActive }
          )
        : await createContact(studentProfileId, body);

      onSaved(saved, isEditing);
    } catch (error) {
      setServerMessage(
        errorText(
          error,
          "Unable to save the contact. Please try again."
        )
      );
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <div
      className="admin-dialog-backdrop"
      role="presentation"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget && !isSaving) {
          onCancel();
        }
      }}
    >
      <section
        className="admin-confirm-dialog leave-dialog tools-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="contact-dialog-title"
      >
        <h2 id="contact-dialog-title">
          {isEditing ? "Edit contact" : "Add contact"}
        </h2>

        {serverMessage && (
          <div className="message error" role="alert">
            {serverMessage}
          </div>
        )}

        <form
          className="tools-dialog-form"
          onSubmit={handleSubmit}
          noValidate
        >
          <div className="admin-form-grid">
            <div className="form-group">
              <label htmlFor="contactType">Type</label>

              <select
                id="contactType"
                name="contactType"
                value={values.contactType}
                onChange={handleChange}
              >
                {CONTACT_TYPES.map((type) => (
                  <option key={type.value} value={type.value}>
                    {type.label}
                  </option>
                ))}
              </select>
            </div>

            <div className="form-group">
              <label htmlFor="contactRelationship">
                Relationship
              </label>

              <input
                id="contactRelationship"
                name="relationship"
                type="text"
                maxLength={100}
                value={values.relationship}
                onChange={handleChange}
                aria-invalid={Boolean(errors.relationship)}
                placeholder="e.g. Mother"
              />

              {errors.relationship && (
                <p className="field-error">
                  {errors.relationship}
                </p>
              )}
            </div>

            <div className="form-group wide">
              <label htmlFor="contactName">Full name</label>

              <input
                id="contactName"
                name="fullName"
                type="text"
                maxLength={200}
                value={values.fullName}
                onChange={handleChange}
                aria-invalid={Boolean(errors.fullName)}
              />

              {errors.fullName && (
                <p className="field-error">{errors.fullName}</p>
              )}
            </div>

            <div className="form-group">
              <label htmlFor="contactPhone">Phone</label>

              <input
                id="contactPhone"
                name="phoneNumber"
                type="tel"
                maxLength={20}
                value={values.phoneNumber}
                onChange={handleChange}
                aria-invalid={Boolean(errors.phoneNumber)}
              />

              {errors.phoneNumber && (
                <p className="field-error">
                  {errors.phoneNumber}
                </p>
              )}
            </div>

            <div className="form-group">
              <label htmlFor="contactAltPhone">
                Alternate phone (optional)
              </label>

              <input
                id="contactAltPhone"
                name="alternatePhone"
                type="tel"
                maxLength={20}
                value={values.alternatePhone}
                onChange={handleChange}
                aria-invalid={Boolean(errors.alternatePhone)}
              />

              {errors.alternatePhone && (
                <p className="field-error">
                  {errors.alternatePhone}
                </p>
              )}
            </div>

            <div className="form-group wide">
              <label htmlFor="contactEmail">
                Email (optional)
              </label>

              <input
                id="contactEmail"
                name="email"
                type="email"
                maxLength={255}
                value={values.email}
                onChange={handleChange}
                aria-invalid={Boolean(errors.email)}
              />

              {errors.email && (
                <p className="field-error">{errors.email}</p>
              )}
            </div>

            <div className="form-group wide">
              <label htmlFor="contactAddress">
                Address (optional)
              </label>

              <textarea
                id="contactAddress"
                name="address"
                maxLength={500}
                value={values.address}
                onChange={handleChange}
              />
            </div>

            <div className="form-group wide">
              <label className="check-row">
                <input
                  name="isPrimary"
                  type="checkbox"
                  checked={values.isPrimary}
                  onChange={handleChange}
                />
                Primary contact
              </label>

              {isEditing && (
                <label className="check-row">
                  <input
                    name="isActive"
                    type="checkbox"
                    checked={values.isActive}
                    onChange={handleChange}
                  />
                  Active
                </label>
              )}
            </div>
          </div>

          <div className="admin-dialog-actions">
            <button
              type="button"
              className="secondary-button"
              disabled={isSaving}
              onClick={onCancel}
            >
              Cancel
            </button>

            <button
              type="submit"
              className="primary-button"
              disabled={isSaving}
            >
              {isSaving ? "Saving..." : "Save contact"}
            </button>
          </div>
        </form>
      </section>
    </div>
  );
}

/* ---------- Page ---------- */

const allowedRoles = [
  "STUDENT",
  "ADMIN",
  "WARDEN",
  "HOSTEL_MASTER"
];

function ContactsPage() {
  const role = sessionStorage.getItem("userRole") ?? "";
  const isStudent = role === "STUDENT";

  const [students, setStudents] = useState([]);
  const [studentProfileId, setStudentProfileId] = useState("");
  const [contacts, setContacts] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");
  const [dialog, setDialog] = useState(null);

  useEffect(() => {
    const token = sessionStorage.getItem("accessToken");

    if (!token || !allowedRoles.includes(role)) {
      window.location.replace("/");
      return;
    }

    async function start() {
      try {
        if (isStudent) {
          const profile = await getOwnProfile();
          setStudentProfileId(String(profile.studentProfileId));
        } else {
          setStudents(await getStudentProfiles());
          setIsLoading(false);
        }
      } catch (error) {
        setErrorMessage(
          errorText(
            error,
            isStudent
              ? "Unable to load your profile."
              : "Unable to load the student list."
          )
        );
        setIsLoading(false);
      }
    }

    start();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    if (studentProfileId) {
      loadContacts(studentProfileId);
    } else {
      setContacts([]);
    }
  }, [studentProfileId]);

  async function loadContacts(profileId) {
    setIsLoading(true);

    try {
      setContacts(await getContacts(profileId));
      setErrorMessage("");
    } catch (error) {
      setContacts([]);
      setErrorMessage(
        errorText(error, "Unable to load the contacts.")
      );
    } finally {
      setIsLoading(false);
    }
  }

  function handleSaved(_saved, wasEditing) {
    setDialog(null);
    setSuccessMessage(
      wasEditing ? "Contact updated." : "Contact added."
    );
    loadContacts(studentProfileId);
  }

  const activePage = "contacts";

  return (
    <AppShell
      activePage={activePage}
      eyebrow="CONTACTS"
      title="Guardian and emergency contacts"
      description={
        isStudent
          ? "Keep your guardian and emergency contact details up to date."
          : "View and maintain a student's guardian and emergency contacts."
      }
      actions={
        studentProfileId && (
          <button
            type="button"
            className="primary-button"
            onClick={() => {
              setSuccessMessage("");
              setDialog({ contact: null });
            }}
          >
            Add contact
          </button>
        )
      }
    >
      <div className="admin-users-page">
        <section className="admin-users-content">
          {successMessage && (
            <div className="message success" role="status">
              {successMessage}
            </div>
          )}

          {errorMessage && (
            <div className="message error" role="alert">
              {errorMessage}
            </div>
          )}

          <section className="admin-users-card">
            <div className="admin-users-toolbar">
              <div>
                <h2>Contacts</h2>

                <span>
                  {studentProfileId
                    ? `${contacts.length} contact${
                        contacts.length === 1 ? "" : "s"
                      }`
                    : "Choose a student to see their contacts"}
                </span>
              </div>

              {!isStudent && (
                <div className="tools-filters">
                  <div className="form-group">
                    <label htmlFor="contactStudent">
                      Student
                    </label>

                    <select
                      id="contactStudent"
                      value={studentProfileId}
                      onChange={(event) => {
                        setSuccessMessage("");
                        setStudentProfileId(event.target.value);
                      }}
                    >
                      <option value="">Select a student</option>
                      {students.map((student) => (
                        <option
                          key={student.studentProfileId}
                          value={student.studentProfileId}
                        >
                          {student.registrationNumber} ·{" "}
                          {student.username}
                        </option>
                      ))}
                    </select>
                  </div>
                </div>
              )}
            </div>

            {!studentProfileId ? (
              isLoading ? (
                <div className="admin-table-state">
                  <div className="loading-spinner" />
                  <p>Loading...</p>
                </div>
              ) : (
                <div className="admin-table-state">
                  <h3>No student selected</h3>
                  <p>Pick a student above to see their contacts.</p>
                </div>
              )
            ) : isLoading ? (
              <div className="admin-table-state">
                <div className="loading-spinner" />
                <p>Loading contacts...</p>
              </div>
            ) : contacts.length === 0 ? (
              <div className="admin-table-state">
                <h3>No contacts yet</h3>
                <p>Add a guardian or emergency contact.</p>
              </div>
            ) : (
              <div className="contact-card-list">
                {contacts.map((contact) => (
                  <article
                    key={contact.guardianContactId}
                    className={
                      contact.isActive
                        ? "contact-card"
                        : "contact-card inactive"
                    }
                  >
                    <div className="contact-card-header">
                      <strong>{contact.fullName}</strong>

                      <div className="contact-card-badges">
                        <span className="role-badge">
                          {contact.contactType === "GUARDIAN"
                            ? "Guardian"
                            : "Emergency"}
                        </span>

                        {contact.isPrimary && (
                          <span className="status-badge active">
                            Primary
                          </span>
                        )}

                        {!contact.isActive && (
                          <span className="status-badge inactive">
                            Inactive
                          </span>
                        )}

                        <button
                          type="button"
                          className="secondary-button"
                          onClick={() => {
                            setSuccessMessage("");
                            setDialog({ contact });
                          }}
                        >
                          Edit
                        </button>
                      </div>
                    </div>

                    <dl>
                      <div>
                        <dt>Relationship</dt>
                        <dd>{contact.relationship}</dd>
                      </div>

                      <div>
                        <dt>Phone</dt>
                        <dd>{contact.phoneNumber}</dd>
                      </div>

                      {contact.alternatePhone && (
                        <div>
                          <dt>Alternate phone</dt>
                          <dd>{contact.alternatePhone}</dd>
                        </div>
                      )}

                      {contact.email && (
                        <div>
                          <dt>Email</dt>
                          <dd>{contact.email}</dd>
                        </div>
                      )}

                      {contact.address && (
                        <div>
                          <dt>Address</dt>
                          <dd>{contact.address}</dd>
                        </div>
                      )}
                    </dl>
                  </article>
                ))}
              </div>
            )}
          </section>
        </section>

        {dialog && (
          <ContactDialog
            contact={dialog.contact}
            studentProfileId={studentProfileId}
            onCancel={() => setDialog(null)}
            onSaved={handleSaved}
          />
        )}
      </div>
    </AppShell>
  );
}

export default ContactsPage;
