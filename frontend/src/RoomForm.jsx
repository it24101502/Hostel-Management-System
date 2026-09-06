import { useState } from "react";

import {
  createRoom,
  RoomApiError,
  updateRoom
} from "./roomApi.js";

function getInitialValues(room) {
  return {
    blockId: room?.blockId?.toString() ?? "",
    floorNumber:
      room?.floorNumber?.toString() ?? "",
    roomNumber: room?.roomNumber ?? "",
    bedCapacity:
      room?.bedCapacity?.toString() ?? ""
  };
}

function RoomForm({ room, onCancel, onSaved }) {
  const isEditing = Boolean(room);
  const [values, setValues] = useState(
    getInitialValues(room)
  );
  const [errors, setErrors] = useState({});
  const [serverMessage, setServerMessage] =
    useState("");
  const [isSubmitting, setIsSubmitting] =
    useState(false);

  function validate() {
    const validationErrors = {};
    const blockId = Number(values.blockId);
    const floorNumber = Number(values.floorNumber);
    const bedCapacity = Number(values.bedCapacity);

    if (!Number.isInteger(blockId) || blockId <= 0) {
      validationErrors.blockId =
        "Enter a valid active block ID.";
    }

    if (
      !Number.isInteger(floorNumber) ||
      floorNumber < 0 ||
      floorNumber > 65535
    ) {
      validationErrors.floorNumber =
        "Floor must be a whole number from 0 to 65535.";
    }

    const roomNumber = values.roomNumber.trim();

    if (!roomNumber) {
      validationErrors.roomNumber =
        "Room number is required.";
    } else if (roomNumber.length > 20) {
      validationErrors.roomNumber =
        "Room number cannot exceed 20 characters.";
    }

    if (
      !Number.isInteger(bedCapacity) ||
      bedCapacity <= 0 ||
      bedCapacity > 65535
    ) {
      validationErrors.bedCapacity =
        "Bed capacity must be a positive whole number.";
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
      const savedRoom = isEditing
        ? await updateRoom(room.roomId, values)
        : await createRoom(values);

      onSaved(
        savedRoom,
        isEditing
          ? `Room ${savedRoom.roomNumber} was updated successfully.`
          : `Room ${savedRoom.roomNumber} was created successfully.`
      );
    } catch (error) {
      if (
        error instanceof RoomApiError &&
        error.status !== 401
      ) {
        setServerMessage(error.message);
      } else {
        setServerMessage(
          "Unable to save the room. Please try again."
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
          <p>{isEditing ? "EDIT ROOM" : "NEW ROOM"}</p>
          <h2>
            {isEditing
              ? `Update room ${room.roomNumber}`
              : "Create a room"}
          </h2>
          <span>
            Enter the block, floor, room number and bed
            capacity.
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
        className="admin-user-form room-form"
        onSubmit={handleSubmit}
        noValidate
      >
        <div className="admin-form-grid">
          <div className="form-group">
            <label htmlFor="roomBlockId">Block ID</label>
            <input
              id="roomBlockId"
              name="blockId"
              type="number"
              min="1"
              step="1"
              value={values.blockId}
              onChange={updateValue}
              aria-invalid={Boolean(errors.blockId)}
              placeholder="Example: 1"
            />
            {errors.blockId && (
              <p className="field-error">
                {errors.blockId}
              </p>
            )}
          </div>

          <div className="form-group">
            <label htmlFor="roomFloorNumber">
              Floor number
            </label>
            <input
              id="roomFloorNumber"
              name="floorNumber"
              type="number"
              min="0"
              max="65535"
              step="1"
              value={values.floorNumber}
              onChange={updateValue}
              aria-invalid={Boolean(errors.floorNumber)}
              placeholder="Example: 1"
            />
            {errors.floorNumber && (
              <p className="field-error">
                {errors.floorNumber}
              </p>
            )}
          </div>

          <div className="form-group">
            <label htmlFor="roomNumber">
              Room number
            </label>
            <input
              id="roomNumber"
              name="roomNumber"
              type="text"
              maxLength="20"
              value={values.roomNumber}
              onChange={updateValue}
              aria-invalid={Boolean(errors.roomNumber)}
              placeholder="Example: 101"
            />
            {errors.roomNumber && (
              <p className="field-error">
                {errors.roomNumber}
              </p>
            )}
          </div>

          <div className="form-group">
            <label htmlFor="roomBedCapacity">
              Bed capacity
            </label>
            <input
              id="roomBedCapacity"
              name="bedCapacity"
              type="number"
              min="1"
              max="65535"
              step="1"
              value={values.bedCapacity}
              onChange={updateValue}
              aria-invalid={Boolean(errors.bedCapacity)}
              placeholder="Example: 4"
            />
            {errors.bedCapacity && (
              <p className="field-error">
                {errors.bedCapacity}
              </p>
            )}
          </div>
        </div>

        <p className="room-form-help">
          Block ID must belong to an active hostel block.
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
              ? "Saving..."
              : isEditing
                ? "Save changes"
                : "Create room"}
          </button>
        </div>
      </form>
    </section>
  );
}

export default RoomForm;
