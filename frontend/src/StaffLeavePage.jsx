import AppShell from "./AppShell.jsx";

import { useEffect, useRef, useState } from "react";

import LeaveDecisionDialog from "./LeaveDecisionDialog.jsx";
import LeaveMovementDialog from "./LeaveMovementDialog.jsx";

import {
  decideLeaveRequest,
  getLeaveReport,
  getStaffNotifications,
  LeaveApiError,
  markNotificationRead,
  recordDeparture,
  recordReturn
} from "./leaveReviewApi.js";

import {
  formatDate,
  formatDateTime,
  statusLabels
} from "./leaveFormat.js";

// Wardens and hostel masters review and act on requests.
// Administrators can only view the report.
const viewingRoles = ["WARDEN", "HOSTEL_MASTER", "ADMIN"];
const actingRoles = ["WARDEN", "HOSTEL_MASTER"];

const REFRESH_INTERVAL_MS = 30000;

const emptyFilters = {
  status: "",
  fromDate: "",
  toDate: ""
};

const emptyTotals = {
  total: 0,
  pending: 0,
  approved: 0,
  rejected: 0,
  departed: 0,
  closed: 0,
  overdue: 0
};

function StaffLeavePage() {
  const role = sessionStorage.getItem("userRole");
  const canAct = actingRoles.includes(role);

  const [report, setReport] = useState({
    totals: emptyTotals,
    requests: []
  });
  const [notifications, setNotifications] =
    useState([]);
  const [filters, setFilters] = useState(emptyFilters);
  const [decisionTarget, setDecisionTarget] =
    useState(null);
  const [movementTarget, setMovementTarget] =
    useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [lastUpdated, setLastUpdated] = useState(null);
  const [errorMessage, setErrorMessage] =
    useState("");
  const [successMessage, setSuccessMessage] =
    useState("");

  // The refresh timer needs the filters that were last applied,
  // not the ones from the render where the timer was created.
  const activeFiltersRef = useRef(emptyFilters);

  useEffect(() => {
    const accessToken =
      sessionStorage.getItem("accessToken");

    if (!accessToken || !viewingRoles.includes(role)) {
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
      setReport(await getLeaveReport(reportFilters));
      setLastUpdated(new Date());
      setErrorMessage("");
    } catch (error) {
      if (
        error instanceof LeaveApiError &&
        error.status !== 401
      ) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage(
          "Unable to load the leave report."
        );
      }
    } finally {
      setIsLoading(false);
    }

    if (canAct) {
      try {
        setNotifications(
          await getStaffNotifications(true)
        );
      } catch {
        // The alerts panel is optional. If it cannot load,
        // the rest of the page still works.
      }
    }
  }

  async function applyFilters(event) {
    event.preventDefault();

    if (
      filters.fromDate &&
      filters.toDate &&
      filters.toDate < filters.fromDate
    ) {
      setErrorMessage(
        "The end date cannot be earlier than the start date."
      );
      return;
    }

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

  async function submitDecision(reason) {
    const { request, decision } = decisionTarget;

    try {
      await decideLeaveRequest(
        request.leaveRequestId,
        decision,
        reason
      );
    } catch (error) {
      // The request may have been changed by someone else, so
      // show the current state behind the dialog.
      loadData(activeFiltersRef.current, true);
      throw error;
    }

    setDecisionTarget(null);
    setErrorMessage("");
    setSuccessMessage(
      `Leave request for ${request.studentUsername} was ` +
        `${decision === "APPROVE" ? "approved" : "rejected"}.`
    );

    await loadData(activeFiltersRef.current, true);
  }

  async function confirmMovement() {
    const { request, kind } = movementTarget;

    try {
      if (kind === "departure") {
        await recordDeparture(request.leaveRequestId);
      } else {
        await recordReturn(request.leaveRequestId);
      }
    } catch (error) {
      loadData(activeFiltersRef.current, true);
      throw error;
    }

    setMovementTarget(null);
    setErrorMessage("");
    setSuccessMessage(
      kind === "departure"
        ? `Departure of ${request.studentUsername} was recorded.`
        : `Return of ${request.studentUsername} was recorded and the request is closed.`
    );

    await loadData(activeFiltersRef.current, true);
  }

  async function handleMarkRead(notification) {
    try {
      await markNotificationRead(
        notification.notificationId
      );

      setNotifications((current) =>
        current.filter(
          (item) =>
            item.notificationId !==
            notification.notificationId
        )
      );
    } catch (error) {
      setErrorMessage(
        error instanceof LeaveApiError &&
          error.status !== 401
          ? error.message
          : "Unable to mark the alert as read."
      );
    }
  }

  const { totals, requests } = report;

  return (
    <AppShell
      activePage="leave"
      eyebrow="LEAVE & MOVEMENT"
      title={
        canAct
          ? "Leave requests"
          : "Leave & movement report"
      }
      description={
        canAct
          ? "Review requests, record departures and returns, and watch for overdue students."
          : "Read-only report of leave requests and student movements."
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

          <section className="allocation-metrics leave-metrics">
            <article>
              <span>Pending review</span>
              <strong>{totals.pending}</strong>
              <small>Waiting for a decision</small>
            </article>

            <article>
              <span>Approved</span>
              <strong>{totals.approved}</strong>
              <small>Not yet departed</small>
            </article>

            <article>
              <span>Currently away</span>
              <strong>{totals.departed}</strong>
              <small>Departed, not returned</small>
            </article>

            <article className="metric-danger">
              <span>Overdue returns</span>
              <strong>{totals.overdue}</strong>
              <small>Past the expected return date</small>
            </article>

            <article>
              <span>Closed</span>
              <strong>{totals.closed}</strong>
              <small>Returned</small>
            </article>

            <article>
              <span>Rejected</span>
              <strong>{totals.rejected}</strong>
              <small>Declined requests</small>
            </article>
          </section>

          {canAct && notifications.length > 0 && (
            <section className="admin-users-card leave-alerts">
              <div className="admin-users-toolbar">
                <div>
                  <h2>Alerts</h2>
                  <span>
                    {notifications.length} unread
                    {notifications.length === 1
                      ? " alert"
                      : " alerts"}
                  </span>
                </div>
              </div>

              <ul className="leave-alert-list">
                {notifications.map((notification) => (
                  <li
                    key={notification.notificationId}
                    className={
                      notification.notificationType ===
                      "RETURN_OVERDUE"
                        ? "leave-alert overdue"
                        : "leave-alert"
                    }
                  >
                    <div>
                      <strong>
                        {notification.notificationType ===
                        "RETURN_OVERDUE"
                          ? "Overdue return"
                          : "New leave request"}
                      </strong>
                      <p>{notification.message}</p>
                      <small>
                        {formatDateTime(
                          notification.createdAt
                        )}
                      </small>
                    </div>

                    <button
                      type="button"
                      className="secondary-button"
                      onClick={() =>
                        handleMarkRead(notification)
                      }
                    >
                      Mark as read
                    </button>
                  </li>
                ))}
              </ul>
            </section>
          )}

          <section className="admin-users-card allocation-card">
            <div className="admin-users-toolbar allocation-toolbar">
              <div>
                <h2>Leave and movement report</h2>
                <span>
                  {requests.length} request
                  {requests.length === 1 ? "" : "s"} shown
                  {" · "}
                  {totals.total} in the selected period
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
                  <option value="PENDING">Pending</option>
                  <option value="APPROVED">Approved</option>
                  <option value="REJECTED">Rejected</option>
                  <option value="DEPARTED">Departed</option>
                  <option value="CLOSED">Closed</option>
                  <option value="OVERDUE">
                    Overdue returns
                  </option>
                </select>

                <input
                  type="date"
                  aria-label="Departing from"
                  value={filters.fromDate}
                  onChange={(event) =>
                    setFilters((current) => ({
                      ...current,
                      fromDate: event.target.value
                    }))
                  }
                />

                <input
                  type="date"
                  aria-label="Departing until"
                  value={filters.toDate}
                  onChange={(event) =>
                    setFilters((current) => ({
                      ...current,
                      toDate: event.target.value
                    }))
                  }
                />

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
                <p>Loading leave requests...</p>
              </div>
            ) : requests.length === 0 ? (
              <div className="admin-table-state">
                <h3>No leave requests found</h3>
                <p>
                  Try a different status or date range.
                </p>
              </div>
            ) : (
              <div className="admin-table-wrapper">
                <table className="admin-users-table leave-table leave-staff-table">
                  <thead>
                    <tr>
                      <th>Student</th>
                      <th>Leave dates</th>
                      <th>Details</th>
                      <th>Status</th>
                      <th>Decision and movement</th>
                      {canAct && (
                        <th>
                          <span className="sr-only">
                            Actions
                          </span>
                        </th>
                      )}
                    </tr>
                  </thead>

                  <tbody>
                    {requests.map((request) => (
                      <tr
                        key={request.leaveRequestId}
                        className={
                          request.isOverdue
                            ? "leave-row-overdue"
                            : undefined
                        }
                      >
                        <td>
                          <div className="room-block-cell">
                            <strong>
                              {request.studentUsername}
                            </strong>
                            <small>
                              #{request.leaveRequestId}
                              {" · "}
                              {formatDateTime(
                                request.createdAt
                              )}
                            </small>
                          </div>
                        </td>

                        <td>
                          <div className="room-block-cell">
                            <strong>
                              {formatDate(
                                request.departureDate
                              )}
                            </strong>
                            <small>
                              to{" "}
                              {formatDate(
                                request.expectedReturnDate
                              )}
                            </small>
                          </div>
                        </td>

                        <td className="leave-reason-cell">
                          <div className="room-block-cell">
                            <span>{request.reason}</span>
                            <small>
                              With {request.companionName}
                              {" ("}
                              {request.companionRelationship}
                              {") · "}
                              {request.companionPhone}
                            </small>
                          </div>
                        </td>

                        <td>
                          <div className="leave-status-stack">
                            <span
                              className={`leave-status ${request.status.toLowerCase()}`}
                            >
                              {statusLabels[request.status] ??
                                request.status}
                            </span>

                            {request.isOverdue && (
                              <span className="leave-status overdue">
                                Overdue
                              </span>
                            )}
                          </div>
                        </td>

                        <td className="leave-reason-cell">
                          <div className="room-block-cell">
                            {request.decisionReason && (
                              <span>
                                {request.decisionReason}
                              </span>
                            )}

                            {request.decidedAt && (
                              <small>
                                Decided{" "}
                                {formatDateTime(
                                  request.decidedAt
                                )}
                              </small>
                            )}

                            {request.actualDepartureAt && (
                              <small>
                                Departed{" "}
                                {formatDateTime(
                                  request.actualDepartureAt
                                )}
                              </small>
                            )}

                            {request.actualReturnAt && (
                              <small>
                                Returned{" "}
                                {formatDateTime(
                                  request.actualReturnAt
                                )}
                              </small>
                            )}

                            {!request.decidedAt && (
                              <small>Not decided yet</small>
                            )}
                          </div>
                        </td>

                        {canAct && (
                          <td>
                            <div className="admin-row-actions">
                              {request.status ===
                                "PENDING" && (
                                <>
                                  <button
                                    type="button"
                                    onClick={() =>
                                      setDecisionTarget({
                                        request,
                                        decision: "APPROVE"
                                      })
                                    }
                                  >
                                    Approve
                                  </button>

                                  <button
                                    type="button"
                                    className="deactivate-button"
                                    onClick={() =>
                                      setDecisionTarget({
                                        request,
                                        decision: "REJECT"
                                      })
                                    }
                                  >
                                    Reject
                                  </button>
                                </>
                              )}

                              {request.status ===
                                "APPROVED" && (
                                <button
                                  type="button"
                                  onClick={() =>
                                    setMovementTarget({
                                      request,
                                      kind: "departure"
                                    })
                                  }
                                >
                                  Record departure
                                </button>
                              )}

                              {request.status ===
                                "DEPARTED" && (
                                <button
                                  type="button"
                                  onClick={() =>
                                    setMovementTarget({
                                      request,
                                      kind: "return"
                                    })
                                  }
                                >
                                  Record return
                                </button>
                              )}
                            </div>
                          </td>
                        )}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </section>
      </div>

      {decisionTarget && (
        <LeaveDecisionDialog
          key={`decision-${decisionTarget.request.leaveRequestId}-${decisionTarget.decision}`}
          request={decisionTarget.request}
          decision={decisionTarget.decision}
          onCancel={() => setDecisionTarget(null)}
          onSubmit={submitDecision}
        />
      )}

      {movementTarget && (
        <LeaveMovementDialog
          key={`movement-${movementTarget.request.leaveRequestId}-${movementTarget.kind}`}
          request={movementTarget.request}
          kind={movementTarget.kind}
          onCancel={() => setMovementTarget(null)}
          onConfirm={confirmMovement}
        />
      )}
    </AppShell>
  );
}

export default StaffLeavePage;
