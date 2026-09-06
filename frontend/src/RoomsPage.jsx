import {
  useEffect,
  useMemo,
  useState
} from "react";

import RoomForm from "./RoomForm.jsx";

import {
  deleteRoom,
  getRooms,
  RoomApiError
} from "./roomApi.js";

function RoomsPage() {
  const [rooms, setRooms] = useState([]);
  const [searchText, setSearchText] = useState("");
  const [panelMode, setPanelMode] = useState(null);
  const [selectedRoom, setSelectedRoom] =
    useState(null);
  const [roomToDelete, setRoomToDelete] =
    useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isDeleting, setIsDeleting] = useState(false);
  const [errorMessage, setErrorMessage] =
    useState("");
  const [successMessage, setSuccessMessage] =
    useState("");

  useEffect(() => {
    const accessToken =
      sessionStorage.getItem("accessToken");
    const role = sessionStorage.getItem("userRole");

    if (!accessToken || role !== "ADMIN") {
      window.location.replace("/");
      return;
    }

    loadRooms();
  }, []);

  async function loadRooms() {
    setIsLoading(true);
    setErrorMessage("");

    try {
      setRooms(await getRooms());
    } catch (error) {
      if (
        error instanceof RoomApiError &&
        error.status !== 401
      ) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage("Unable to load rooms.");
      }
    } finally {
      setIsLoading(false);
    }
  }

  const filteredRooms = useMemo(() => {
    const query = searchText.trim().toLowerCase();

    if (!query) {
      return rooms;
    }

    return rooms.filter((room) =>
      [
        room.blockCode,
        room.blockName,
        room.floorNumber?.toString(),
        room.roomNumber,
        room.bedCapacity?.toString()
      ]
        .filter(Boolean)
        .some((value) =>
          value.toLowerCase().includes(query)
        )
    );
  }, [rooms, searchText]);

  function openCreateForm() {
    setSelectedRoom(null);
    setPanelMode("create");
    setSuccessMessage("");
    setErrorMessage("");
  }

  function openEditForm(room) {
    setSelectedRoom(room);
    setPanelMode("edit");
    setSuccessMessage("");
    setErrorMessage("");
  }

  function closePanel() {
    setSelectedRoom(null);
    setPanelMode(null);
  }

  function handleRoomSaved(savedRoom, message) {
    setRooms((currentRooms) => {
      const exists = currentRooms.some(
        (room) => room.roomId === savedRoom.roomId
      );

      if (exists) {
        return currentRooms.map((room) =>
          room.roomId === savedRoom.roomId
            ? savedRoom
            : room
        );
      }

      return [savedRoom, ...currentRooms];
    });

    closePanel();
    setSuccessMessage(message);

    window.scrollTo({
      top: 0,
      behavior: "smooth"
    });
  }

  async function confirmDelete() {
    if (!roomToDelete) {
      return;
    }

    setIsDeleting(true);
    setErrorMessage("");

    try {
      await deleteRoom(roomToDelete.roomId);

      setRooms((currentRooms) =>
        currentRooms.filter(
          (room) => room.roomId !== roomToDelete.roomId
        )
      );

      setSuccessMessage(
        `Room ${roomToDelete.roomNumber} was deleted successfully.`
      );
      setRoomToDelete(null);
      closePanel();
    } catch (error) {
      if (
        error instanceof RoomApiError &&
        error.status !== 401
      ) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage(
          "Unable to delete the room. Please try again."
        );
      }

      setRoomToDelete(null);
    } finally {
      setIsDeleting(false);
    }
  }

  function handleLogout() {
    sessionStorage.clear();
    window.location.replace("/");
  }

  return (
    <main className="admin-users-page">
      <header className="admin-header">
        <div className="admin-brand">
          <span>HMS</span>

          <div>
            <strong>Hostel Management System</strong>
            <small>Administrator Portal</small>
          </div>
        </div>

        <nav>
          <button
            type="button"
            className="secondary-button"
            onClick={() =>
              window.location.assign("/admin")
            }
          >
            Dashboard
          </button>

          <button
            type="button"
            className="secondary-button"
            onClick={() =>
              window.location.assign("/admin/users")
            }
          >
            Users
          </button>

          <button
            type="button"
            className="danger-button"
            onClick={handleLogout}
          >
            Sign out
          </button>
        </nav>
      </header>

      <section className="admin-users-content">
        <div className="admin-title-row">
          <div>
            <p>ROOM MANAGEMENT</p>
            <h1>Hostel rooms</h1>
            <span>
              Create, view, update and delete room records.
            </span>
          </div>

          <button
            type="button"
            className="primary-button"
            onClick={openCreateForm}
          >
            + Create room
          </button>
        </div>

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

        {panelMode === "create" && (
          <RoomForm
            key="create-room"
            onCancel={closePanel}
            onSaved={handleRoomSaved}
          />
        )}

        {panelMode === "edit" && selectedRoom && (
          <RoomForm
            key={`edit-${selectedRoom.roomId}`}
            room={selectedRoom}
            onCancel={closePanel}
            onSaved={handleRoomSaved}
          />
        )}

        <section className="admin-users-card">
          <div className="admin-users-toolbar">
            <div>
              <h2>All rooms</h2>
              <span>
                {filteredRooms.length} room
                {filteredRooms.length === 1 ? "" : "s"}
              </span>
            </div>

            <div className="admin-search">
              <label
                htmlFor="roomSearch"
                className="sr-only"
              >
                Search rooms
              </label>
              <input
                id="roomSearch"
                type="search"
                value={searchText}
                onChange={(event) =>
                  setSearchText(event.target.value)
                }
                placeholder="Search block, floor or room"
              />
            </div>
          </div>

          {isLoading ? (
            <div className="admin-table-state">
              <div className="loading-spinner" />
              <p>Loading rooms...</p>
            </div>
          ) : filteredRooms.length === 0 ? (
            <div className="admin-table-state">
              <h3>No rooms found</h3>
              <p>
                Try changing your search or create a room.
              </p>
            </div>
          ) : (
            <div className="admin-table-wrapper">
              <table className="admin-users-table room-table">
                <thead>
                  <tr>
                    <th>Block</th>
                    <th>Floor</th>
                    <th>Room</th>
                    <th>Bed capacity</th>
                    <th>Status</th>
                    <th>
                      <span className="sr-only">Actions</span>
                    </th>
                  </tr>
                </thead>

                <tbody>
                  {filteredRooms.map((room) => (
                    <tr key={room.roomId}>
                      <td>
                        <div className="room-block-cell">
                          <strong>{room.blockCode}</strong>
                          <small>{room.blockName}</small>
                        </div>
                      </td>
                      <td>{room.floorNumber}</td>
                      <td>
                        <strong>{room.roomNumber}</strong>
                      </td>
                      <td>
                        <span className="capacity-badge">
                          {room.bedCapacity} beds
                        </span>
                      </td>
                      <td>
                        <span
                          className={
                            room.isActive
                              ? "status-badge active"
                              : "status-badge inactive"
                          }
                        >
                          {room.isActive
                            ? "Active"
                            : "Inactive"}
                        </span>
                      </td>
                      <td>
                        <div className="admin-row-actions">
                          <button
                            type="button"
                            onClick={() => openEditForm(room)}
                          >
                            Edit
                          </button>
                          <button
                            type="button"
                            className="deactivate-button"
                            onClick={() =>
                              setRoomToDelete(room)
                            }
                          >
                            Delete
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      </section>

      {roomToDelete && (
        <div
          className="admin-dialog-backdrop"
          role="presentation"
          onMouseDown={(event) => {
            if (event.target === event.currentTarget) {
              setRoomToDelete(null);
            }
          }}
        >
          <section
            className="admin-confirm-dialog"
            role="alertdialog"
            aria-modal="true"
            aria-labelledby="delete-room-title"
          >
            <div className="admin-warning-icon">!</div>
            <h2 id="delete-room-title">Delete room?</h2>
            <p>
              Room <strong>{roomToDelete.roomNumber}</strong>{" "}
              will be permanently deleted. An occupied room
              cannot be deleted.
            </p>

            <div className="admin-dialog-actions">
              <button
                type="button"
                className="secondary-button"
                disabled={isDeleting}
                onClick={() => setRoomToDelete(null)}
              >
                Cancel
              </button>
              <button
                type="button"
                className="danger-button"
                disabled={isDeleting}
                onClick={confirmDelete}
              >
                {isDeleting ? "Deleting..." : "Delete room"}
              </button>
            </div>
          </section>
        </div>
      )}
    </main>
  );
}

export default RoomsPage;
