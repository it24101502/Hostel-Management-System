import { useState } from "react";

import {
  AdminApiError,
  createAdminUser,
  createStudentProfile,
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
      roleId: user?.roleId?.toString() ?? "4",
      registrationNumber: "",
      dateOfBirth: "",
      gender: "",
      addressLine1: "",
      addressLine2: "",
      city: "",
      district: "",
      postalCode: "",
      programmeName: "",
      facultyName: "",
      academicYear: ""
    });

  const [validationErrors, setValidationErrors] =
    useState({});

  const [errorMessage, setErrorMessage] =
    useState("");

  const [isSaving, setIsSaving] =
    useState(false);

  const [
    pendingCreatedUser,
    setPendingCreatedUser
  ] = useState(null);

  const isCreatingStudent =
    !isEditing &&
    (
      pendingCreatedUser !== null ||
      formData.roleId === "4"
    );

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
      !pendingCreatedUser &&
      formData.password.length < 8
    ) {
      errors.password =
        "Password must contain at least 8 characters.";
    }

    if (!formData.roleId) {
      errors.roleId = "Select a role.";
    }

    if (isCreatingStudent) {
      if (!formData.registrationNumber.trim()) {
        errors.registrationNumber =
          "Registration number is required.";
      }

      if (!formData.programmeName.trim()) {
        errors.programmeName =
          "Programme name is required.";
      }

      if (!formData.facultyName.trim()) {
        errors.facultyName =
          "Faculty name is required.";
      }

      const academicYear =
        Number(formData.academicYear);

      if (
        !formData.academicYear ||
        !Number.isInteger(academicYear) ||
        academicYear < 1 ||
        academicYear > 10
      ) {
        errors.academicYear =
          "Academic year must be between 1 and 10.";
      }
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

    let createdUserForProfile =
      pendingCreatedUser;

    try {
      let savedUser;

      if (isEditing) {
        savedUser = await updateAdminUser(
          user.userId,
          formData
        );
      } else {
        savedUser =
          pendingCreatedUser ??
          await createAdminUser(formData);

        if (isCreatingStudent) {
          createdUserForProfile = savedUser;
          setPendingCreatedUser(savedUser);

          await createStudentProfile(
            savedUser.userId,
            {
              ...formData,
              email: savedUser.email
            }
          );
        }
      }

      setPendingCreatedUser(null);

      onSaved(
        savedUser,
        isEditing
          ? "User account updated successfully."
          : isCreatingStudent
            ? "Student account and profile created successfully."
            : "User account created successfully."
      );
    } catch (error) {
      const apiMessage =
        error instanceof AdminApiError &&
        error.status !== 401
          ? error.message
          : "The request could not be completed.";

      if (
        createdUserForProfile &&
        isCreatingStudent
      ) {
        setPendingCreatedUser(
          createdUserForProfile
        );

        setErrorMessage(
          "The user account was created, but the " +
          "student profile could not be created. " +
          apiMessage +
          " Correct the profile information and " +
          "submit again; the account will not be duplicated."
        );
      } else {
        setErrorMessage(
          error instanceof AdminApiError &&
          error.status !== 401
            ? error.message
            : "Unable to save the user account. Please try again."
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
          {isCreatingStudent && (
            <>
              <div className="admin-password-field">
                <h3>Student profile</h3>
                <span>
                  These details will be linked to the new
                  Student account.
                </span>
              </div>

              <div className="form-group">
                <label htmlFor="registrationNumber">
                  Registration number
                </label>

                <input
                  id="registrationNumber"
                  name="registrationNumber"
                  type="text"
                  maxLength={50}
                  value={formData.registrationNumber}
                  onChange={handleChange}
                  aria-invalid={Boolean(
                    validationErrors.registrationNumber
                  )}
                  placeholder="IT26000001"
                />

                {validationErrors.registrationNumber && (
                  <p className="field-error">
                    {
                      validationErrors
                        .registrationNumber
                    }
                  </p>
                )}
              </div>

              <div className="form-group">
                <label htmlFor="dateOfBirth">
                  Date of birth
                </label>

                <input
                  id="dateOfBirth"
                  name="dateOfBirth"
                  type="date"
                  value={formData.dateOfBirth}
                  onChange={handleChange}
                />
              </div>

              <div className="form-group">
                <label htmlFor="gender">
                  Gender
                </label>

                <select
                  id="gender"
                  name="gender"
                  value={formData.gender}
                  onChange={handleChange}
                >
                  <option value="">Not provided</option>
                  <option value="FEMALE">Female</option>
                  <option value="MALE">Male</option>
                  <option value="OTHER">Other</option>
                </select>
              </div>

              <div className="form-group">
                <label htmlFor="academicYear">
                  Academic year
                </label>

                <input
                  id="academicYear"
                  name="academicYear"
                  type="number"
                  min="1"
                  max="10"
                  value={formData.academicYear}
                  onChange={handleChange}
                  aria-invalid={Boolean(
                    validationErrors.academicYear
                  )}
                  placeholder="3"
                />

                {validationErrors.academicYear && (
                  <p className="field-error">
                    {validationErrors.academicYear}
                  </p>
                )}
              </div>

              <div className="form-group">
                <label htmlFor="programmeName">
                  Programme name
                </label>

                <input
                  id="programmeName"
                  name="programmeName"
                  type="text"
                  maxLength={150}
                  value={formData.programmeName}
                  onChange={handleChange}
                  aria-invalid={Boolean(
                    validationErrors.programmeName
                  )}
                  placeholder="BSc (Hons) in Information Technology"
                />

                {validationErrors.programmeName && (
                  <p className="field-error">
                    {validationErrors.programmeName}
                  </p>
                )}
              </div>

              <div className="form-group">
                <label htmlFor="facultyName">
                  Faculty name
                </label>

                <input
                  id="facultyName"
                  name="facultyName"
                  type="text"
                  maxLength={150}
                  value={formData.facultyName}
                  onChange={handleChange}
                  aria-invalid={Boolean(
                    validationErrors.facultyName
                  )}
                  placeholder="Faculty of Computing"
                />

                {validationErrors.facultyName && (
                  <p className="field-error">
                    {validationErrors.facultyName}
                  </p>
                )}
              </div>

              <div className="form-group">
                <label htmlFor="addressLine1">
                  Address line 1
                </label>

                <input
                  id="addressLine1"
                  name="addressLine1"
                  type="text"
                  maxLength={255}
                  value={formData.addressLine1}
                  onChange={handleChange}
                />
              </div>

              <div className="form-group">
                <label htmlFor="addressLine2">
                  Address line 2
                </label>

                <input
                  id="addressLine2"
                  name="addressLine2"
                  type="text"
                  maxLength={255}
                  value={formData.addressLine2}
                  onChange={handleChange}
                />
              </div>

              <div className="form-group">
                <label htmlFor="city">
                  City
                </label>

                <input
                  id="city"
                  name="city"
                  type="text"
                  maxLength={100}
                  value={formData.city}
                  onChange={handleChange}
                />
              </div>

              <div className="form-group">
                <label htmlFor="district">
                  District
                </label>

                <input
                  id="district"
                  name="district"
                  type="text"
                  maxLength={100}
                  value={formData.district}
                  onChange={handleChange}
                />
              </div>

              <div className="form-group">
                <label htmlFor="postalCode">
                  Postal code
                </label>

                <input
                  id="postalCode"
                  name="postalCode"
                  type="text"
                  maxLength={20}
                  value={formData.postalCode}
                  onChange={handleChange}
                />
              </div>
            </>
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
              : pendingCreatedUser
                ? "Retry profile creation"
                : isEditing
                  ? "Save changes"
                  : isCreatingStudent
                    ? "Create student account"
                    : "Create account"}
          </button>
        </div>
      </form>
    </section>
  );
}

export default AdminUserForm;