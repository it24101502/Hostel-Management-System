import AppShell from "./AppShell.jsx";

import { useEffect, useRef, useState } from "react";

import {
  ComplaintAssignDialog,
  ComplaintStatusDialog
} from "./ComplaintDialogs.jsx";

import {
  assignComplaint,
  changeComplaintStatus,
  ComplaintApiError,
  downloadComplaintReportCsv,
  getComplaintReport
} from "./complaintApi.js";

import {
  categoryLabels,
  complaintCategories,
  complaintStatusLabels
} from "./complaintFormat.js";

import { formatDateTime } from "./leaveFormat.js";

const staffRoles = ["WARDEN", "HOSTEL_MASTER", "ADMIN"];

const REFRESH_INTERVAL_MS = 30000;

const emptyFilters = { status: "", category: "" };

const emptyTotals = {
  total: 0,
  open: 0,
  inProgress: 0,
  resolved: 0
};

function StaffComplaintsPage() {
  const role = sessionStorage.getItem("userRole");

  const [report, setReport] = useState({
    totals: emptyTotals,
    complaints: []
  });
  const [filters, setFilters] = useState(emptyFilters);
  const [assignTarget, setAssignTarget] =
    useState(null);
  const [statusTarget, setStatusTarget] =
    useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isDownloading, setIsDownloading] =
    useState(false);
  const [lastUpdated, setLastUpdated] = useState(null);
  const [errorMessage, setErrorMessage] =
    useState("");
  const [successMessage, setSuccessMessage] =
    useState("");

  // The refresh timer needs the filters that were last applied.
  const activeFiltersRef = useRef(emptyFilters);

  useEffect(() => {
    const accessToken =
      sessionStorage.getItem("accessToken");

    if (!accessToken || !staffRoles.includes(role)) {
      window.location.replace("/");
      return undefined;
    }

    loadData(emptyFilters, false);

    const timer = window.setInterval(
      () => loadData(activeFiltersRef.current, true),
      REFRESH_INTERVAL_MS
    );

    return () => window.clearInterval(timer);
  }, []);

  async function loadData(reportFilters, isBackgroundRefresh) {
    if (!isBackgroundRefresh) {
      setIsLoading(true);
    }

    try {
      setReport(await getComplaintReport(reportFilters));
      setLastUpdated(new Date());
      setErrorMessage("");
    } catch (error) {
      if (
        error instanceof ComplaintApiError &&
        error.status !== 401
      ) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage("Unable to load complaints.");
      }
    } finally {
      setIsLoading(false);
    }
  }

  async function applyFilters(event) {
    event.preventDefault();
    setSuccessMessage("");
    activeFiltersRef.current = { ...filters };
    await loadData(activeFiltersRef.current, false);
  }

  async function clearFilters() {
    setFilters(emptyFilters);
    setSuccessMessage("");
    activeFiltersRef.current = emptyFilters;
    await loadData(emptyFilters, false);
  }

  async function submitAssign(assigneeId) {
    const complaint = assignTarget;

    try {
      await assignComplaint(
        complaint.complaintId,
        assigneeId
      );
    } catch (error) {
      loadData(activeFiltersRef.current, true);
      throw error;
    }

    setAssignTarget(null);
    setErrorMessage("");
    setSuccessMessage(
      `Complaint #${complaint.complaintId} was assigned.`
    );

    await loadData(activeFiltersRef.current, true);
  }

  async function submitStatus(status, remarks) {
    const complaint = statusTarget;

    try {
      await changeComplaintStatus(
        complaint.complaintId,
        status,
        remarks
      );
    } catch (error) {
      loadData(activeFiltersRef.current, true);
      throw error;
    }

    setStatusTarget(null);
    setErrorMessage("");
    setSuccessMessage(
      `Complaint #${complaint.complaintId} is now ${complaintStatusLabels[status].toLowerCase()}. The student was notified.`
    );

    await loadData(activeFiltersRef.current, true);
  }

  async function handleDownload() {
    setIsDownloading(true);
    setErrorMessage("");

    try {
      await downloadComplaintReportCsv(
        activeFiltersRef.current
      );
    } catch (error) {
      setErrorMessage(
        error instanceof ComplaintApiError &&
          error.status !== 401
          ? error.message
          : "Unable to download the report."
      );
    } finally {
      setIsDownloading(false);
    }
  }

  const { totals, complaints } = report;

  return (
    <AppShell
      activePage="complaints"
      eyebrow="COMPLAINTS"
      title="Complaint management"
      description="Review complaints, assign them to staff and update their status."
      actions={
        <button
          type="button"
          className="secondary-button"
          disabled={isDownloading}
          onClick={handleDownload}
        >
          {isDownloading ? "Preparing..." : "Download CSV"}
        </button>
      }
    >
      <div className="admin-users-page leave-page">
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

          <section className="allocation-metrics">
            <article>
              <span>Total complaints</span>
              <strong>{totals.total}</strong>
              <small>In the selected category</small>
            </article>

            <article>
              <span>Open</span>
              <strong>{totals.open}</strong>
              <small>Waiting for staff</small>
            </article>

            <article>
              <span>In progress</span>
              <strong>{totals.inProgress}</strong>
              <small>Being handled</small>
            </article>

            <article className="metric-accent">
              <span>Resolved</span>
              <strong>{totals.resolved}</strong>
              <small>Completed</small>
            </article>
          </section>

          <section className="admin-users-card allocation-card">
            <div className="admin-users-toolbar allocation-toolbar">
              <div>
                <h2>All complaints</h2>
                <span>
                  {complaints.length} shown
                  {lastUpdated
                    ? ` · updated ${formatDateTime(lastUpdated)}`
                    : ""}
                </span>
              </div>

              <form
                className="occupancy-filters leave-filters"
                onSubmit={applyFilters}
              >
                <select
                  aria-label="Filter by status"
                  value={filters.status}
                  onChange={(event) =>
                    setFilters((current) => ({
                      ...current,
                      status: event.target.value
                    }))
                  }
                >
                  <option value="">All statuses</option>
                  {Object.entries(complaintStatusLabels).map(
                    ([value, label]) => (
                      <option key={value} value={value}>
                        {label}
                      </option>
                    )
                  )}
                </select>

                <select
                  aria-label="Filter by category"
                  value={filters.category}
                  onChange={(event) =>
                    setFilters((current) => ({
                      ...current,
                      category: event.target.value
                    }))
                  }
                >
                  <option value="">All categories</option>
                  {complaintCategories.map((category) => (
                    <option
                      key={category.value}
                      value={category.value}
                    >
                      {category.label}
                    </option>
                  ))}
                </select>

                <button
                  type="submit"
                  className="primary-button"
                >
                  Apply
                </button>

                <button
                  type="button"
                  className="secondary-button"
                  onClick={clearFilters}
                >
                  Clear
                </button>
              </form>
            </div>

            {isLoading ? (
              <div className="admin-table-state">
                <div className="loading-spinner" />
                <p>Loading complaints...</p>
              </div>
            ) : complaints.length === 0 ? (
              <div className="admin-table-state">
                <h3>No complaints found</h3>
                <p>Try a different status or category.</p>
              </div>
            ) : (
              <div className="admin-table-wrapper">
                <table className="admin-users-table leave-table complaint-table">
                  <thead>
                    <tr>
                      <th>Student</th>
                      <th>Category</th>
                      <th>Description</th>
                      <th>Status</th>
                      <th>Assigned to</th>
                      <th>
                        <span className="sr-only">
                          Actions
                        </span>
                      </th>
                    </tr>
                  </thead>

                  <tbody>
                    {complaints.map((item) => (
                      <tr key={item.complaintId}>
                        <td>
                          <div className="room-block-cell">
                            <strong>
                              {item.studentUsername}
                            </strong>
                            <small>
                              #{item.complaintId}
                              {" · "}
                              {formatDateTime(item.createdAt)}
                            </small>
                          </div>
                        </td>

                        <td>
                          <span className="role-badge">
                            {categoryLabels[item.category] ??
                              item.category}
                          </span>
                        </td>

                        <td className="complaint-description-cell">
                          {item.description}
                        </td>

                        <td>
                          <span
                            className={`complaint-status ${item.status.toLowerCase()}`}
                          >
                            {complaintStatusLabels[item.status] ??
                              item.status}
                          </span>
                        </td>

                        <td>
                          <div className="room-block-cell">
                            <strong>
                              {item.assignedToUserId
                                ? `User #${item.assignedToUserId}`
                                : "Unassigned"}
                            </strong>

                            {item.assignedAt && (
                              <small>
                                {formatDateTime(item.assignedAt)}
                              </small>
                            )}
                          </div>
                        </td>

                        <td>
                          <div className="admin-row-actions">
                            <button
                              type="button"
                              onClick={() =>
                                setAssignTarget(item)
                              }
                            >
                              {item.assignedToUserId
                                ? "Reassign"
                                : "Assign"}
                            </button>

                            <button
                              type="button"
                              onClick={() =>
                                setStatusTarget(item)
                              }
                            >
                              Change status
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

      {assignTarget && (
        <ComplaintAssignDialog
          key={`assign-${assignTarget.complaintId}`}
          complaint={assignTarget}
          onCancel={() => setAssignTarget(null)}
          onSubmit={submitAssign}
        />
      )}

      {statusTarget && (
        <ComplaintStatusDialog
          key={`status-${statusTarget.complaintId}-${statusTarget.status}`}
          complaint={statusTarget}
          onCancel={() => setStatusTarget(null)}
          onSubmit={submitStatus}
        />
      )}
    </AppShell>
  );
}

export default StaffComplaintsPage;