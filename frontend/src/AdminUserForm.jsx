import { useState } from "react";

import {
  AdminApiError,
  createAdminUser,
  updateAdminUser
} from "./adminUserApi.js";

const roles = [
  {
    roleId: 1,
    roleName: "Administrator"
  },
  {
    roleId: 2,
    roleName: "Warden"
  },
  {
    roleId: 3,
    roleName: "Hostel Master"
  },
  {
    roleId: 4,
    roleName: "Student"
  }
];

function AdminUserForm({
  user,
  onCancel,
  onSaved
}) {
  const isEditing = Boolean(user);

  const [formData, setFormData] =
    useState({
      username: user?.username ?? "",
      email: user?.email ?? "",
      firstName: user?.firstName ?? "",
      lastName: user?.lastName ?? "",
      phoneNumber: user?.phoneNumber ?? "",
      password: "",
      roleId: user?.roleId?.toString() ?? "4"
    });

  const [validationErrors, setValidationErrors] =
    useState({});

  const [errorMessage, setErrorMessage] =
    useState("");

  const [isSaving, setIsSaving] =
    useState(false);

  function handleChange(event) {
    const { name, value } = event.target;

    setFormData((current) => ({
      ...current,
      [name]: value
    }));

    setValidationErrors((current) => ({
      ...current,
      [name]: ""
    }));

    setErrorMessage("");
  }

  function validateForm() {
    const errors = {};

    if (formData.username.trim().length < 3) {
      errors.username =
        "Username must contain at least 3 characters.";
    }

    if (!formData.email.trim()) {
      errors.email =
        "Email address is required.";
    } else if (
      !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(
        formData.email.trim()
      )
    ) {
      errors.email =
        "Enter a valid email address.";
    }

    if (formData.firstName.trim().length < 2) {
      errors.firstName =
        "First name must contain at least 2 characters.";
    }

    if (formData.lastName.trim().length < 2) {
      errors.lastName =
        "Last name must contain at least 2 characters.";
    }

    if (
      formData.phoneNumber.trim() &&
      !/^[0-9+\-\s()]{7,20}$/.test(
        formData.phoneNumber.trim()
      )
    ) {
      errors.phoneNumber =
        "Enter a valid phone number.";
    }

    if (
      !isEditing &&
      formData.password.length < 8
    ) {
      errors.password =
        "Password must contain at least 8 characters.";
    }

    if (!formData.roleId) {
      errors.roleId = "Select a role.";
    }

    setValidationErrors(errors);

    return Object.keys(errors).length === 0;
  }

  async function handleSubmit(event) {
    event.preventDefault();

    setErrorMessage("");

    if (!validateForm()) {
      return;
    }

    setIsSaving(true);

    try {
      const savedUser = isEditing
        ? await updateAdminUser(
            user.userId,
            formData
          )
        : await createAdminUser(formData);

      onSaved(
        savedUser,
        isEditing
          ? "User account updated successfully."
          : "User account created successfully."
      );
    } catch (error) {
      if (
        error instanceof AdminApiError &&
        error.status !== 401
      ) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage(
          "Unable to save the user account. Please try again."
        );
      }
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <section className="admin-form-panel">
      <div className="admin-panel-heading">
        <div>
          <p>
            {isEditing
              ? "EDIT ACCOUNT"
              : "NEW ACCOUNT"}
          </p>

          <h2>
            {isEditing
              ? "Update user account"
              : "Create user account"}
          </h2>

          <span>
            {isEditing
              ? "Update the permitted account information."
              : "Enter the information required for the new account."}
          </span>
        </div>

        <button
          type="button"
          className="secondary-button"
          onClick={onCancel}
          disabled={isSaving}
        >
          Close
        </button>
      </div>

      {errorMessage && (
        <div
          className="message error"
          role="alert"
        >
          {errorMessage}
        </div>
      )}

      <form
        className="admin-user-form"
        onSubmit={handleSubmit}
        noValidate
      >
        <div className="admin-form-grid">
          <div className="form-group">
            <label htmlFor="adminUsername">
              Username
            </label>

            <input
              id="adminUsername"
              name="username"
              type="text"
              maxLength={50}
              value={formData.username}
              onChange={handleChange}
              aria-invalid={Boolean(
                validationErrors.username
              )}
              placeholder="Enter username"
            />

            {validationErrors.username && (
              <p className="field-error">
                {validationErrors.username}
              </p>
            )}
          </div>

          <div className="form-group">
            <label htmlFor="adminEmail">
              Email address
            </label>

            <input
              id="adminEmail"
              name="email"
              type="email"
              maxLength={255}
              value={formData.email}
              onChange={handleChange}
              aria-invalid={Boolean(
                validationErrors.email
              )}
              placeholder="user@example.com"
            />

            {validationErrors.email && (
              <p className="field-error">
                {validationErrors.email}
              </p>
            )}
          </div>

          <div className="form-group">
            <label htmlFor="adminFirstName">
              First name
            </label>

            <input
              id="adminFirstName"
              name="firstName"
              type="text"
              maxLength={100}
              value={formData.firstName}
              onChange={handleChange}
              aria-invalid={Boolean(
                validationErrors.firstName
              )}
              placeholder="Enter first name"
            />

            {validationErrors.firstName && (
              <p className="field-error">
                {validationErrors.firstName}
              </p>
            )}
          </div>

          <div className="form-group">
            <label htmlFor="adminLastName">
              Last name
            </label>

            <input
              id="adminLastName"
              name="lastName"
              type="text"
              maxLength={100}
              value={formData.lastName}
              onChange={handleChange}
              aria-invalid={Boolean(
                validationErrors.lastName
              )}
              placeholder="Enter last name"
            />

            {validationErrors.lastName && (
              <p className="field-error">
                {validationErrors.lastName}
              </p>
            )}
          </div>

          <div className="form-group">
            <label htmlFor="adminPhoneNumber">
              Phone number
            </label>

            <input
              id="adminPhoneNumber"
              name="phoneNumber"
              type="tel"
              maxLength={20}
              value={formData.phoneNumber}
              onChange={handleChange}
              aria-invalid={Boolean(
                validationErrors.phoneNumber
              )}
              placeholder="0771234567"
            />

            {validationErrors.phoneNumber && (
              <p className="field-error">
                {validationErrors.phoneNumber}
              </p>
            )}
          </div>

          <div className="form-group">
            <label htmlFor="adminRole">
              Role
            </label>

            <select
              id="adminRole"
              name="roleId"
              value={formData.roleId}
              onChange={handleChange}
              aria-invalid={Boolean(
                validationErrors.roleId
              )}
            >
              {roles.map((role) => (
                <option
                  key={role.roleId}
                  value={role.roleId}
                >
                  {role.roleName}
                </option>
              ))}
            </select>

            {validationErrors.roleId && (
              <p className="field-error">
                {validationErrors.roleId}
              </p>
            )}
          </div>

          {!isEditing && (
            <div className="form-group admin-password-field">
              <label htmlFor="adminPassword">
                Temporary password
              </label>

              <input
                id="adminPassword"
                name="password"
                type="password"
                minLength={8}
                maxLength={100}
                autoComplete="new-password"
                value={formData.password}
                onChange={handleChange}
                aria-invalid={Boolean(
                  validationErrors.password
                )}
                placeholder="Minimum 8 characters"
              />

              {validationErrors.password && (
                <p className="field-error">
                  {validationErrors.password}
                </p>
              )}
            </div>
          )}
        </div>

        <div className="admin-form-actions">
          <button
            type="button"
            className="secondary-button"
            onClick={onCancel}
            disabled={isSaving}
          >
            Cancel
          </button>

          <button
            type="submit"
            className="primary-button"
            disabled={isSaving}
          >
            {isSaving
              ? "Saving..."
              : isEditing
                ? "Save changes"
                : "Create account"}
          </button>
        </div>
      </form>
    </section>
  );
}

export default AdminUserForm;