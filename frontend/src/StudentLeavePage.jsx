import AppShell from "./AppShell.jsx";

import { useEffect, useMemo, useState } from "react";

import LeaveRequestForm from "./LeaveRequestForm.jsx";

import {
  getMyLeaveRequests,
  LeaveApiError
} from "./leaveApi.js";

import {
  formatDate,
  formatDateTime,
  statusLabels
} from "./leaveFormat.js";

// How often the list refreshes itself so students see
// approvals and rejections without reloading the page.
const REFRESH_INTERVAL_MS = 15000;

function StudentLeavePage() {
  const [requests, setRequests] = useState([]);
  const [showForm, setShowForm] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [lastUpdated, setLastUpdated] = useState(null);
  const [errorMessage, setErrorMessage] =
    useState("");
  const [successMessage, setSuccessMessage] =
    useState("");

  useEffect(() => {
    const accessToken =
      sessionStorage.getItem("accessToken");
    const role = sessionStorage.getItem("userRole");

    if (!accessToken || role !== "STUDENT") {
      window.location.replace("/");
      return undefined;
    }

    loadRequests(false);

    const timer = window.setInterval(
      () => loadRequests(true),
      REFRESH_INTERVAL_MS
    );

    return () => window.clearInterval(timer);
  }, []);

  async function loadRequests(isBackgroundRefresh) {
    if (!isBackgroundRefresh) {
      setIsLoading(true);
    }

    try {
      setRequests(await getMyLeaveRequests());
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
          "Unable to load your leave requests."
        );
      }
    } finally {
      setIsLoading(false);
    }
  }

  function handleSaved(savedRequest, message) {
    setShowForm(false);
    setErrorMessage("");
    setSuccessMessage(message);
    loadRequests(false);
  }

  function openForm() {
    setSuccessMessage("");
    setShowForm(true);
  }

  const metrics = useMemo(
    () => ({
      total: requests.length,
      pending: requests.filter(
        (request) => request.status === "PENDING"
      ).length,
      approved: requests.filter(
        (request) =>
          request.status === "APPROVED" ||
          request.status === "DEPARTED"
      ).length,
      closed: requests.filter(
        (request) => request.status === "CLOSED"
      ).length
    }),
    [requests]
  );

  return (
    <AppShell
      activePage="leave"
      eyebrow="LEAVE & MOVEMENT"
      title="My leave requests"
      description="Submit a leave request and follow its approval status."
      actions={
        <button
          type="button"
          className="primary-button"
          onClick={openForm}
        >
          + New leave request
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
              <span>Total requests</span>
              <strong>{metrics.total}</strong>
              <small>All submitted requests</small>
            </article>

            <article>
              <span>Pending</span>
              <strong>{metrics.pending}</strong>
              <small>Waiting for a decision</small>
            </article>

            <article>
              <span>Approved</span>
              <strong>{metrics.approved}</strong>
              <small>Approved or currently away</small>
            </article>

            <article className="metric-accent">
              <span>Closed</span>
              <strong>{metrics.closed}</strong>
              <small>Completed leave</small>
            </article>
          </section>

          {showForm && (
            <LeaveRequestForm
              onCancel={() => setShowForm(false)}
              onSaved={handleSaved}
            />
          )}

          <section className="admin-users-card">
            <div className="admin-users-toolbar">
              <div>
                <h2>Leave history</h2>
                <span>
                  {requests.length} request
                  {requests.length === 1 ? "" : "s"}
                  {lastUpdated
                    ? ` · updated ${formatDateTime(lastUpdated)}`
                    : ""}
                </span>
              </div>

              <button
                type="button"
                className="secondary-button"
                disabled={isLoading}
                onClick={() => loadRequests(false)}
              >
                Refresh
              </button>
            </div>

            {isLoading ? (
              <div className="admin-table-state">
                <div className="loading-spinner" />
                <p>Loading your leave requests...</p>
              </div>
            ) : requests.length === 0 ? (
              <div className="admin-table-state">
                <h3>No leave requests yet</h3>
                <p>
                  Select “New leave request” to submit your
                  first request.
                </p>
              </div>
            ) : (
              <div className="admin-table-wrapper">
                <table className="admin-users-table leave-table">
                  <thead>
                    <tr>
                      <th>Request</th>
                      <th>Leave dates</th>
                      <th>Reason</th>
                      <th>Companion</th>
                      <th>Status</th>
                    </tr>
                  </thead>

                  <tbody>
                    {requests.map((request) => (
                      <tr key={request.leaveRequestId}>
                        <td>
                          <div className="room-block-cell">
                            <strong>
                              #{request.leaveRequestId}
                            </strong>
                            <small>
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
                          {request.reason}
                        </td>

                        <td>
                          <div className="room-block-cell">
                            <strong>
                              {request.companionName}
                            </strong>
                            <small>
                              {request.companionRelationship}
                              {" · "}
                              {request.companionPhone}
                            </small>
                          </div>
                        </td>

                        <td>
                          <span
                            className={`leave-status ${request.status.toLowerCase()}`}
                          >
                            {statusLabels[request.status] ??
                              request.status}
                          </span>
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

export default StudentLeavePage;
