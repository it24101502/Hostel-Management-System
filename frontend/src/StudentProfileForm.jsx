import {
  useEffect,
  useRef,
  useState
} from "react";

import {
  ApiError,
  updateOwnStudentProfile,
  uploadOwnStudentPhoto
} from "./studentProfileApi.js";

import ProfilePhotoCropper from
  "./ProfilePhotoCropper.jsx";

const maximumPhotoSize =
  2 * 1024 * 1024;

const acceptedPhotoTypes = [
  "image/jpeg",
  "image/png",
  "image/webp"
];

function StudentProfileForm({
  profile,
  onCancel,
  onSaved
}) {
  const [formData, setFormData] = useState({
    addressLine1: profile.addressLine1 ?? "",
    addressLine2: profile.addressLine2 ?? "",
    city: profile.city ?? "",
    district: profile.district ?? "",
    postalCode: profile.postalCode ?? ""
  });

  const [selectedPhoto, setSelectedPhoto] =
    useState(null);

  const [photoToAdjust, setPhotoToAdjust] =
    useState(null);

  const [previewUrl, setPreviewUrl] =
    useState("");

  const [photoError, setPhotoError] =
    useState("");

  const [errorMessage, setErrorMessage] =
    useState("");

  const [isSaving, setIsSaving] =
    useState(false);

  const [isDragging, setIsDragging] =
    useState(false);

  const fileInputRef = useRef(null);

  useEffect(() => {
    return () => {
      if (previewUrl) {
        URL.revokeObjectURL(previewUrl);
      }
    };
  }, [previewUrl]);

  function handleChange(event) {
    const { name, value } = event.target;

    setFormData((current) => ({
      ...current,
      [name]: value
    }));

    setErrorMessage("");
  }

  function selectPhoto(file) {
    setPhotoError("");

    if (!file) {
      return;
    }

    if (!acceptedPhotoTypes.includes(file.type)) {
      setPhotoError(
        "Please select a JPG, PNG or WebP image."
      );

      return;
    }

    if (file.size > maximumPhotoSize) {
      setPhotoError(
        "The selected photo must not exceed 2 MB."
      );

      return;
    }

    if (previewUrl) {
      URL.revokeObjectURL(previewUrl);
    }

    setSelectedPhoto(null);
    setPreviewUrl("");
    setPhotoToAdjust(file);
  }

  function applyAdjustedPhoto(adjustedPhoto) {
    if (previewUrl) {
      URL.revokeObjectURL(previewUrl);
    }

    setSelectedPhoto(adjustedPhoto);

    setPreviewUrl(
      URL.createObjectURL(adjustedPhoto)
    );

    setPhotoToAdjust(null);
    setPhotoError("");
  }

  function cancelPhotoAdjustment() {
    setPhotoToAdjust(null);

    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
  }

  function handleFileChange(event) {
    selectPhoto(event.target.files?.[0]);
  }

  function handleDragOver(event) {
    event.preventDefault();
    setIsDragging(true);
  }

  function handleDragLeave(event) {
    event.preventDefault();
    setIsDragging(false);
  }

  function handleDrop(event) {
    event.preventDefault();
    setIsDragging(false);

    selectPhoto(
      event.dataTransfer.files?.[0]
    );
  }

  function clearSelectedPhoto() {
    if (previewUrl) {
      URL.revokeObjectURL(previewUrl);
    }

    setSelectedPhoto(null);
    setPhotoToAdjust(null);
    setPreviewUrl("");
    setPhotoError("");

    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
  }

  async function handleSubmit(event) {
    event.preventDefault();

    setErrorMessage("");
    setIsSaving(true);

    try {
      let updatedProfile =
        await updateOwnStudentProfile(
          formData
        );

      if (selectedPhoto) {
        updatedProfile =
          await uploadOwnStudentPhoto(
            selectedPhoto
          );
      }

      onSaved(updatedProfile);
    } catch (error) {
      if (
        error instanceof ApiError &&
        error.status !== 401
      ) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage(
          "Unable to update your profile. Please try again."
        );
      }
    } finally {
      setIsSaving(false);
    }
  }

  const displayedPhoto =
    previewUrl || profile.profilePhotoUrl;

  return (
    <form
      className="profile-edit-form"
      onSubmit={handleSubmit}
    >
      <div className="profile-card-heading">
        <div>
          <p>EDIT PROFILE</p>
          <h2>Update your profile</h2>

          <span>
            You can update your profile picture
            and permitted address information.
          </span>
        </div>
      </div>

      {errorMessage && (
        <div
          className="message error"
          role="alert"
        >
          {errorMessage}
        </div>
      )}

      <section className="photo-upload-section">
        <div className="photo-upload-heading">
          <div>
            <h3>Profile picture</h3>

            <p>
              Upload one JPG, PNG or WebP image.
              Maximum size: 2 MB.
            </p>
          </div>

          <div className="photo-preview">
            {displayedPhoto ? (
              <img
                key={displayedPhoto}
                src={
                  previewUrl
                    ? displayedPhoto
                    : `${displayedPhoto}?v=${encodeURIComponent(
                        profile.updatedAt ?? Date.now()
                      )}`
                }
                alt="Profile preview"
              />
            ) : (
              <span>
                {profile.registrationNumber
                  ?.slice(0, 2)
                  .toUpperCase() || "ST"}
              </span>
            )}
          </div>
        </div>

        <input
          ref={fileInputRef}
          id="profilePhoto"
          name="profilePhoto"
          type="file"
          accept="image/jpeg,image/png,image/webp"
          className="photo-file-input"
          onChange={handleFileChange}
        />

        {photoToAdjust ? (
          <ProfilePhotoCropper
            file={photoToAdjust}
            onApply={applyAdjustedPhoto}
            onCancel={cancelPhotoAdjustment}
          />
        ) : (
          <>
            <div
              className={
                isDragging
                  ? "photo-drop-zone dragging"
                  : "photo-drop-zone"
              }
              role="button"
              tabIndex={0}
              onClick={() =>
                fileInputRef.current?.click()
              }
              onKeyDown={(event) => {
                if (
                  event.key === "Enter" ||
                  event.key === " "
                ) {
                  event.preventDefault();
                  fileInputRef.current?.click();
                }
              }}
              onDragOver={handleDragOver}
              onDragLeave={handleDragLeave}
              onDrop={handleDrop}
            >
              <div
                className="photo-upload-icon"
                aria-hidden="true"
              >
                ↑
              </div>

              <strong>
                Drag and drop your photo here
              </strong>

              <span>
                or select it from your device library
              </span>

              <button
                type="button"
                className="photo-choose-button"
                onClick={(event) => {
                  event.stopPropagation();
                  fileInputRef.current?.click();
                }}
              >
                Choose photo
              </button>
            </div>

            {selectedPhoto && (
              <div className="selected-photo-row">
                <img
                  src={previewUrl}
                  alt="Adjusted profile preview"
                />

                <div>
                  <strong>{selectedPhoto.name}</strong>

                  <span>
                    {(selectedPhoto.size / 1024).toFixed(1)} KB
                  </span>
                </div>

                <button
                  type="button"
                  className="photo-remove-button"
                  onClick={clearSelectedPhoto}
                >
                  Remove
                </button>
              </div>
            )}
          </>
        )}

        {photoError && (
          <p
            className="field-error"
            role="alert"
          >
            {photoError}
          </p>
        )}
      </section>

      <div className="profile-form-grid">
        <div className="form-group">
          <label htmlFor="addressLine1">
            Address line 1
          </label>

          <input
            id="addressLine1"
            name="addressLine1"
            type="text"
            maxLength="255"
            value={formData.addressLine1}
            onChange={handleChange}
            placeholder="Enter your address"
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
            maxLength="255"
            value={formData.addressLine2}
            onChange={handleChange}
            placeholder="Apartment or building"
          />
        </div>

        <div className="form-group">
          <label htmlFor="city">City</label>

          <input
            id="city"
            name="city"
            type="text"
            maxLength="100"
            value={formData.city}
            onChange={handleChange}
            placeholder="Enter your city"
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
            maxLength="100"
            value={formData.district}
            onChange={handleChange}
            placeholder="Enter your district"
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
            maxLength="20"
            value={formData.postalCode}
            onChange={handleChange}
            placeholder="Enter your postal code"
          />
        </div>
      </div>

      <div className="profile-form-actions">
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
          disabled={isSaving || Boolean(photoError)}
        >
          {isSaving
            ? "Saving changes..."
            : "Save changes"}
        </button>
      </div>
    </form>
  );
}

export default StudentProfileForm;