import AppShell from "./AppShell.jsx";

import { useEffect, useMemo, useState } from "react";
import {
  allocateStudent,
  AllocationApiError,
  getAllocations,
  getOccupancyReport,
  transferStudent
} from "./allocationApi.js";
import { getRooms } from "./roomApi.js";
import { getActiveStudentProfiles } from
  "./studentProfileApi.js";

const emptyFilters = { blockId: "", floorNumber: "" };

function AllocationsPage() {
  const [allocations, setAllocations] = useState([]);
  const [students, setStudents] = useState([]);
  const [rooms, setRooms] = useState([]);
  const [allOccupancy, setAllOccupancy] = useState([]);
  const [occupancy, setOccupancy] = useState([]);
  const [filters, setFilters] = useState(emptyFilters);
  const [activeFilters, setActiveFilters] = useState(emptyFilters);
  const [panelMode, setPanelMode] = useState(null);
  const [selectedAllocation, setSelectedAllocation] = useState(null);
  const [studentId, setStudentId] = useState("");
  const [roomId, setRoomId] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  useEffect(() => {
    const token = sessionStorage.getItem("accessToken");
    const role = sessionStorage.getItem("userRole");

    if (!token || role !== "ADMIN") {
      window.location.replace("/");
      return;
    }

    loadData(emptyFilters);
  }, []);

  async function loadData(reportFilters) {
    setIsLoading(true);
    setErrorMessage("");

    try {
      const [
        allocationData,
        roomData,
        allReport,
        filteredReport,
        studentData
      ] = await Promise.all([
        getAllocations(),
        getRooms(),
        getOccupancyReport(),
        getOccupancyReport(reportFilters),
        getActiveStudentProfiles()
      ]);

      setAllocations(allocationData);
      setRooms(roomData);
      setAllOccupancy(allReport);
      setOccupancy(filteredReport);
      setStudents(studentData);
    } catch (error) {
      setErrorMessage(
        error instanceof AllocationApiError && error.status !== 401
          ? error.message
          : "Unable to load accommodation information."
      );
    } finally {
      setIsLoading(false);
    }
  }

  const occupancyByRoom = useMemo(
    () => new Map(allOccupancy.map((room) => [room.roomId, room])),
    [allOccupancy]
  );

  const availableRooms = useMemo(
    () =>
      rooms.filter((room) => {
        const liveRoom = occupancyByRoom.get(room.roomId);
        return room.isActive && liveRoom?.availableBeds > 0;
      }),
    [rooms, occupancyByRoom]
  );

  const studentById = useMemo(
    () =>
      new Map(
        students.map((student) => [
          Number(student.studentProfileId),
          student
        ])
      ),
    [students]
  );

  const availableStudents = useMemo(() => {
    const allocatedStudentIds = new Set(
      allocations.map((allocation) =>
        Number(allocation.studentProfileId)
      )
    );

    return students.filter(
      (student) =>
        !allocatedStudentIds.has(
          Number(student.studentProfileId)
        )
    );
  }, [students, allocations]);

  const selectedStudent =
    studentById.get(Number(studentId));

  const blocks = useMemo(() => {
    const uniqueBlocks = new Map();
    rooms.forEach((room) =>
      uniqueBlocks.set(room.blockId, {
        id: room.blockId,
        label: `${room.blockCode} — ${room.blockName}`
      })
    );
    return [...uniqueBlocks.values()];
  }, [rooms]);

  const floors = useMemo(() => {
    const blockId = Number(filters.blockId);
    return [...new Set(
      rooms
        .filter((room) => !blockId || room.blockId === blockId)
        .map((room) => room.floorNumber)
    )].sort((a, b) => a - b);
  }, [rooms, filters.blockId]);

  const metrics = useMemo(
    () => ({
      rooms: occupancy.length,
      beds: occupancy.reduce((sum, room) => sum + room.bedCapacity, 0),
      occupied: occupancy.reduce(
        (sum, room) => sum + room.currentOccupancy,
        0
      ),
      available: occupancy.reduce(
        (sum, room) => sum + room.availableBeds,
        0
      )
    }),
    [occupancy]
  );

  function openAllocate() {
    setPanelMode("allocate");
    setSelectedAllocation(null);
    setStudentId("");
    setRoomId("");
    setErrorMessage("");
    setSuccessMessage("");
  }

  function openTransfer(allocation) {
    setPanelMode("transfer");
    setSelectedAllocation(allocation);
    setStudentId(String(allocation.studentProfileId));
    setRoomId("");
    setErrorMessage("");
    setSuccessMessage("");
  }

  function closePanel() {
    setPanelMode(null);
    setSelectedAllocation(null);
    setStudentId("");
    setRoomId("");
  }

  async function saveAllocation(event) {
    event.preventDefault();
    setErrorMessage("");
    setSuccessMessage("");

    if (!studentId || !roomId) {
      setErrorMessage("Enter a student ID and select a room.");
      return;
    }

    setIsSaving(true);
    try {
      if (panelMode === "allocate") {
        await allocateStudent(studentId, roomId);
        setSuccessMessage(`Student ${studentId} was allocated successfully.`);
      } else {
        await transferStudent(selectedAllocation.studentProfileId, roomId);
        setSuccessMessage(
          `Student ${selectedAllocation.studentProfileId} was transferred successfully.`
        );
      }

      closePanel();
      await loadData(activeFilters);
    } catch (error) {
      setErrorMessage(
        error instanceof AllocationApiError && error.status !== 401
          ? error.message
          : "Unable to save the allocation. Please try again."
      );
    } finally {
      setIsSaving(false);
    }
  }

  async function applyFilters(event) {
    event.preventDefault();
    const nextFilters = { ...filters };
    setActiveFilters(nextFilters);
    await loadData(nextFilters);
  }

  async function clearFilters() {
    setFilters(emptyFilters);
    setActiveFilters(emptyFilters);
    await loadData(emptyFilters);
  }

  function logout() {
    sessionStorage.clear();
    window.location.replace("/");
  }

  return (
    <AppShell
      activePage="allocations"
      eyebrow="ACCOMMODATION CONTROL"
      title="Allocation Command Centre"
      description="Manage student room allocations and monitor live occupancy."
      actions={
        <button
          type="button"
          className="primary-button"
          onClick={openAllocate}
        >
          + New allocation
        </button>
      }
    >
      <div className="admin-users-page allocation-page">
      <header className="admin-header">
        <div className="admin-brand">
          <span>HMS</span>
          <div>
            <strong>Hostel Management System</strong>
            <small>Accommodation Operations</small>
          </div>
        </div>
        <nav>
          <button className="secondary-button" onClick={() => window.location.assign("/admin")}>Dashboard</button>
          <button className="secondary-button" onClick={() => window.location.assign("/admin/rooms")}>Rooms</button>
          <button className="secondary-button" onClick={() => window.location.assign("/admin/users")}>Users</button>
          <button className="nav-active" type="button">Allocations</button>
          <button className="danger-button" onClick={logout}>Sign out</button>
        </nav>
      </header>

      <section className="admin-users-content allocation-content">
        <div className="admin-title-row allocation-hero">
          <div>
            <p>ACCOMMODATION CONTROL</p>
            <h1>Allocation Command Centre</h1>
            <span>Manage room allocations and monitor live occupancy.</span>
          </div>
          <button className="primary-button" onClick={openAllocate}>
            + New allocation
          </button>
        </div>

        {successMessage && <div className="message success">{successMessage}</div>}
        {errorMessage && <div className="message error">{errorMessage}</div>}

        <section className="allocation-metrics">
          <article><span>Rooms</span><strong>{metrics.rooms}</strong><small>Matching filters</small></article>
          <article><span>Total beds</span><strong>{metrics.beds}</strong><small>Maximum capacity</small></article>
          <article><span>Occupied beds</span><strong>{metrics.occupied}</strong><small>Current allocations</small></article>
          <article className="metric-accent"><span>Available beds</span><strong>{metrics.available}</strong><small>Ready for allocation</small></article>
        </section>

        {panelMode && (
          <section className="admin-form-panel allocation-form-panel">
            <div className="admin-panel-heading">
              <div>
                <p>{panelMode === "allocate" ? "NEW ALLOCATION" : "ROOM TRANSFER"}</p>
                <h2>{panelMode === "allocate" ? "Allocate a student" : `Transfer student ${studentId}`}</h2>
                <span>Only active rooms with available beds are shown.</span>
              </div>
              <button className="secondary-button" onClick={closePanel}>Close</button>
            </div>
            <form onSubmit={saveAllocation}>
              <div className="admin-form-grid">
                <div className="form-group">
                  <label htmlFor="studentId">Student</label>

                  {panelMode === "transfer" ? (
                    <input
                      id="studentId"
                      type="text"
                      disabled
                      value={
                        selectedStudent
                          ? selectedStudent.username
                          : `Profile #${studentId}`
                      }
                    />
                  ) : (
                    <select
                      id="studentId"
                      required
                      value={studentId}
                      onChange={(event) =>
                        setStudentId(event.target.value)
                      }
                    >
                      <option value="">
                        {availableStudents.length > 0
                          ? "Select an active student"
                          : "No unallocated students available"}
                      </option>

                      {availableStudents.map((student) => (
                        <option
                          key={student.studentProfileId}
                          value={student.studentProfileId}
                        >
                          {student.username}
                        </option>
                      ))}
                    </select>
                  )}
                </div>
                <div className="form-group">
                  <label htmlFor="roomId">Destination room</label>
                  <select id="roomId" required value={roomId} onChange={(event) => setRoomId(event.target.value)}>
                    <option value="">Select an available room</option>
                    {availableRooms
                      .filter((room) => !selectedAllocation || room.roomId !== selectedAllocation.roomId)
                      .map((room) => {
                        const available = occupancyByRoom.get(room.roomId)?.availableBeds ?? 0;
                        return <option key={room.roomId} value={room.roomId}>{room.blockCode} · Floor {room.floorNumber} · Room {room.roomNumber} · {available} free</option>;
                      })}
                  </select>
                </div>
              </div>
              <div className="admin-form-actions">
                <button type="button" className="secondary-button" disabled={isSaving} onClick={closePanel}>Cancel</button>
                <button type="submit" className="primary-button" disabled={isSaving}>{isSaving ? "Saving..." : panelMode === "allocate" ? "Allocate student" : "Confirm transfer"}</button>
              </div>
            </form>
          </section>
        )}

        <section className="admin-users-card allocation-card">
          <div className="admin-users-toolbar allocation-toolbar">
            <div>
              <h2>Live occupancy</h2>
              <span>Real-time room availability by block and floor.</span>
            </div>
            <form className="occupancy-filters" onSubmit={applyFilters}>
              <select
                aria-label="Filter by block"
                value={filters.blockId}
                onChange={(event) =>
                  setFilters({
                    blockId: event.target.value,
                    floorNumber: ""
                  })
                }
              >
                <option value="">All blocks</option>
                {blocks.map((block) => (
                  <option key={block.id} value={block.id}>
                    {block.label}
                  </option>
                ))}
              </select>
              <select
                aria-label="Filter by floor"
                value={filters.floorNumber}
                onChange={(event) =>
                  setFilters((current) => ({
                    ...current,
                    floorNumber: event.target.value
                  }))
                }
              >
                <option value="">All floors</option>
                {floors.map((floor) => (
                  <option key={floor} value={floor}>
                    Floor {floor}
                  </option>
                ))}
              </select>
              <button type="submit" className="primary-button">Apply</button>
              <button type="button" className="secondary-button" onClick={clearFilters}>Clear</button>
            </form>
          </div>

          {isLoading ? (
            <div className="admin-table-state">
              <div className="loading-spinner" />
              <p>Loading live occupancy...</p>
            </div>
          ) : occupancy.length === 0 ? (
            <div className="admin-table-state">
              <h3>No rooms match these filters</h3>
              <p>Choose another block or floor.</p>
            </div>
          ) : (
            <div className="admin-table-wrapper">
              <table className="admin-users-table occupancy-table">
                <thead>
                  <tr>
                    <th>Room</th>
                    <th>Block</th>
                    <th>Floor</th>
                    <th>Total beds</th>
                    <th>Occupied</th>
                    <th>Available</th>
                    <th>Occupancy</th>
                    <th>Status</th>
                  </tr>
                </thead>
                <tbody>
                  {occupancy.map((room) => {
                    const rate = room.bedCapacity
                      ? Math.round((room.currentOccupancy / room.bedCapacity) * 100)
                      : 0;
                    return (
                      <tr key={room.roomId}>
                        <td><strong>{room.roomNumber}</strong></td>
                        <td>
                          <div className="room-block-cell">
                            <strong>{room.blockCode}</strong>
                            <small>{room.blockName}</small>
                          </div>
                        </td>
                        <td>{room.floorNumber}</td>
                        <td>{room.bedCapacity}</td>
                        <td>{room.currentOccupancy}</td>
                        <td><strong>{room.availableBeds}</strong></td>
                        <td>
                          <div className="occupancy-rate">
                            <span>{rate}%</span>
                            <div><i style={{ width: `${rate}%` }} /></div>
                          </div>
                        </td>
                        <td>
                          <span className={`occupancy-status ${room.status.toLowerCase()}`}>
                            {room.status}
                          </span>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </section>

        <section className="admin-users-card allocation-card">
          <div className="admin-users-toolbar">
            <div>
              <h2>Student allocations</h2>
              <span>
                {allocations.length} active allocation
                {allocations.length === 1 ? "" : "s"}
              </span>
            </div>
          </div>

          {isLoading ? (
            <div className="admin-table-state">
              <div className="loading-spinner" />
              <p>Loading allocations...</p>
            </div>
          ) : allocations.length === 0 ? (
            <div className="admin-table-state">
              <h3>No active allocations</h3>
              <p>Allocate a student to begin.</p>
            </div>
          ) : (
            <div className="admin-table-wrapper">
              <table className="admin-users-table allocation-table">
                <thead>
                  <tr>
                    <th>Student</th>
                    <th>Room</th>
                    <th>Block</th>
                    <th>Floor</th>
                    <th>Occupancy</th>
                    <th>Status</th>
                    <th><span className="sr-only">Actions</span></th>
                  </tr>
                </thead>
                <tbody>
                  {allocations.map((allocation) => (
                    <tr key={allocation.allocationId}>
                      <td>
                        <div className="room-block-cell">
                          <strong>
                            {studentById.get(
                              Number(allocation.studentProfileId)
                            )?.username ??
                              `Profile #${allocation.studentProfileId}`}
                          </strong>

                          <small>
                            {studentById.get(
                              Number(allocation.studentProfileId)
                            )?.email ??
                              "Identity record unavailable"}
                          </small>
                        </div>
                      </td>
                      <td>{allocation.roomNumber}</td>
                      <td>{allocation.blockCode}</td>
                      <td>{allocation.floorNumber}</td>
                      <td>{allocation.currentOccupancy}/{allocation.bedCapacity}</td>
                      <td>
                        <span className={`occupancy-status ${allocation.status.toLowerCase()}`}>
                          {allocation.status}
                        </span>
                      </td>
                      <td>
                        <div className="admin-row-actions">
                          <button type="button" onClick={() => openTransfer(allocation)}>
                            Transfer
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
      </div>
    </AppShell>
  );
}

export default AllocationsPage;
