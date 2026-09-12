import AppShell from "./AppShell.jsx";

import { useEffect, useState } from "react";
import {
  ApiError,
  getOwnStudentProfile
} from "./studentProfileApi.js";
import StudentProfileForm from
  "./StudentProfileForm.jsx";

function formatDate(value) {
  if (!value) {
    return "Not provided";
  }

  return new Intl.DateTimeFormat("en-GB", {
    year: "numeric",
    month: "long",
    day: "numeric"
  }).format(new Date(value));
}

function displayValue(value) {
  return value || "Not provided";
}

function ProfileField({ label, value }) {
  return (
    <div className="profile-field">
      <span>{label}</span>
      <strong>{displayValue(value)}</strong>
    </div>
  );
}

function StudentProfilePage() {
  const [profile, setProfile] = useState(null);
  const [errorMessage, setErrorMessage] =
    useState("");
  const [isLoading, setIsLoading] =
    useState(true);
  const [isEditing, setIsEditing] =
    useState(false);
  const [successMessage, setSuccessMessage] =
    useState("");
  const [photoLoadFailed, setPhotoLoadFailed] =
  useState(false);

  useEffect(() => {
    const role =
      sessionStorage.getItem("userRole");

    const accessToken =
      sessionStorage.getItem("accessToken");

    if (!accessToken || role !== "STUDENT") {
      window.location.replace("/");
      return;
    }

    async function loadProfile() {
      try {
        const data =
          await getOwnStudentProfile();

        setProfile(data);
      } catch (error) {
        if (
          error instanceof ApiError &&
          error.status !== 401
        ) {
          setErrorMessage(error.message);
        }
      } finally {
        setIsLoading(false);
      }
    }

    loadProfile();
  }, []);

  function handleProfileSaved(updatedProfile) {
    setProfile(updatedProfile);
    setIsEditing(false);

    setSuccessMessage(
      "Your profile was updated successfully."
    );

    window.scrollTo({
      top: 0,
      behavior: "smooth"
    });
  }

  function handleLogout() {
    sessionStorage.clear();
    window.location.replace("/");
  }

  if (isLoading) {
    return (
      <AppShell
        activePage="profile"
        eyebrow="MY ACCOUNT"
        title="Student Profile"
        description="View your information and update permitted contact fields."
      >
        <section className="profile-shell-state">
          <div className="profile-shell-state-card">
            <div className="loading-spinner" />
            <h2>Loading your profile</h2>
            <p>Please wait while we retrieve your information.</p>
          </div>
        </section>
      </AppShell>
    );
  }

  if (errorMessage || !profile) {
    return (
      <AppShell
        activePage="profile"
        eyebrow="MY ACCOUNT"
        title="Student Profile"
        description="View your information and update permitted contact fields."
      >
        <section className="profile-shell-state">
          <div className="profile-shell-state-card">
            <h2>Unable to load profile</h2>

            <p>
              {errorMessage ||
                "Your student profile could not be found."}
            </p>

            <button
              type="button"
              className="primary-button"
              onClick={() => window.location.reload()}
            >
              Try again
            </button>
          </div>
        </section>
      </AppShell>
    );
  }
    return (
      <AppShell
        activePage="profile"
        eyebrow="MY ACCOUNT"
        title="Student Profile"
        description="View your information and update permitted contact fields."
        actions={
          !isEditing ? (
            <button
              type="button"
              className="primary-button"
              onClick={() => {
                setSuccessMessage("");
                setIsEditing(true);
              }}
            >
              Edit profile
            </button>
          ) : null
        }
      >
      <div className="profile-page">
        <section className="profile-content">
          {successMessage && (
            <div
              className="message success"
              role="status"
            >
              {successMessage}
            </div>
          )}

        <section className="profile-summary-card">
          <div className="profile-avatar">
            {profile.profilePhotoUrl && !photoLoadFailed ? (
              <img
                key={profile.profilePhotoUrl}
                src={`${profile.profilePhotoUrl}?v=${encodeURIComponent(
                  profile.updatedAt ?? Date.now()
                )}`}
                alt="Student profile"
                onError={() => setPhotoLoadFailed(true)}
              />
            ) : (
              <span>
                {profile.registrationNumber
                  ?.slice(0, 2)
                  .toUpperCase() || "ST"}
              </span>
            )}
          </div>

          <div>
            <h2>{profile.registrationNumber}</h2>
            <p>{profile.email}</p>

            <span className="profile-status">
              Active student
            </span>
          </div>
        </section>

        {isEditing ? (
          <section className="profile-card profile-card-wide">
            <StudentProfileForm
              profile={profile}
              onCancel={() => setIsEditing(false)}
              onSaved={handleProfileSaved}
            />
          </section>
        ) : (

        <div className="profile-grid">
          <section className="profile-card">
            <div className="profile-card-heading">
              <div>
                <p>PERSONAL DETAILS</p>
                <h2>Personal information</h2>
              </div>
            </div>

            <div className="profile-fields-grid">
              <ProfileField
                label="Email address"
                value={profile.email}
              />

              <ProfileField
                label="Registration number"
                value={
                  profile.registrationNumber
                }
              />

              <ProfileField
                label="Date of birth"
                value={formatDate(
                  profile.dateOfBirth
                )}
              />

              <ProfileField
                label="Gender"
                value={profile.gender}
              />
            </div>
          </section>

          <section className="profile-card">
            <div className="profile-card-heading">
              <div>
                <p>ACADEMIC DETAILS</p>
                <h2>Academic information</h2>
              </div>
            </div>

            <div className="profile-fields-grid">
              <ProfileField
                label="Programme"
                value={profile.programmeName}
              />

              <ProfileField
                label="Faculty"
                value={profile.facultyName}
              />

              <ProfileField
                label="Academic year"
                value={
                  profile.academicYear
                    ? `Year ${profile.academicYear}`
                    : null
                }
              />
            </div>
          </section>

          <section className="profile-card profile-card-wide">
            <div className="profile-card-heading">
              <div>
                <p>CONTACT DETAILS</p>
                <h2>Address information</h2>
              </div>
            </div>

            <div className="profile-fields-grid">
              <ProfileField
                label="Address line 1"
                value={profile.addressLine1}
              />

              <ProfileField
                label="Address line 2"
                value={profile.addressLine2}
              />

              <ProfileField
                label="City"
                value={profile.city}
              />

              <ProfileField
                label="District"
                value={profile.district}
              />

              <ProfileField
                label="Postal code"
                value={profile.postalCode}
              />

            </div>
          </section>
        </div>
        )}
    </section>
  </div>
</AppShell>
);
}

export default StudentProfilePage;