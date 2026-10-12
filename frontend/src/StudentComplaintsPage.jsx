import AppShell from "./AppShell.jsx";

import { useEffect, useMemo, useState } from "react";

import ComplaintForm from "./ComplaintForm.jsx";

import {
  ComplaintApiError,
  getMyComplaints,
  getMyNotifications,
  markNotificationRead
} from "./complaintApi.js";

import {
  categoryLabels,
  complaintStatusLabels
} from "./complaintFormat.js";

import { formatDateTime } from "./leaveFormat.js";

const REFRESH_INTERVAL_MS = 15000;

function StudentComplaintsPage() {
  const [complaints, setComplaints] = useState([]);
  const [notifications, setNotifications] = useState([]);
  const [showForm, setShowForm] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [lastUpdated, setLastUpdated] = useState(null);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  useEffect(() => {
    const accessToken = sessionStorage.getItem("accessToken");
    const role = sessionStorage.getItem("userRole");

    if (!accessToken || role !== "STUDENT") {
      window.location.replace("/");
      return undefined;
    }

    loadData(false);

    const timer = window.setInterval(
      () => loadData(true),
      REFRESH_INTERVAL_MS
    );

    return () => window.clearInterval(timer);
  }, []);

  async function loadData(isBackgroundRefresh) {
    if (!isBackgroundRefresh) {
      setIsLoading(true);
    }

    try {
      setComplaints(await getMyComplaints());
      setLastUpdated(new Date());
      setErrorMessage("");
    } catch (error) {
      if (
        error instanceof ComplaintApiError &&
        error.status !== 401
      ) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage(
          "Unable to load your complaints."
        );
      }
    } finally {
      setIsLoading(false);
    }

    try {
      setNotifications(await getMyNotifications());
    } catch {
      // Notifications are optional; the page still works.
    }
  }

  async function handleMarkRead(item) {
    try {
      await markNotificationRead(item.notificationId);
      setNotifications((current) =>
        current.filter(
          (n) => n.notificationId !== item.notificationId
        )
      );
    } catch (error) {
      setErrorMessage(
        error instanceof ComplaintApiError && error.status !== 401
          ? error.message
          : "Unable to mark the update as read."
      );
    }
  }

  function handleSaved(saved, message) {
    setShowForm(false);
    setErrorMessage("");
    setSuccessMessage(message);
    loadData(false);
  }

  function openForm() {
    setSuccessMessage("");
    setShowForm(true);
  }

  const metrics = useMemo(
    () => ({
      total: complaints.length,
      open: complaints.filter(
        (item) => item.status === "OPEN"
      ).length,
      inProgress: complaints.filter(
        (item) => item.status === "IN_PROGRESS"
      ).length,
      resolved: complaints.filter(
        (item) => item.status === "RESOLVED"
      ).length
    }),
    [complaints]
  );

  const unreadNotifications = notifications.filter(
    (item) => !item.isRead
  );

  return (
    <AppShell
      activePage="complaints"
      eyebrow="COMPLAINTS"
      title="My complaints"
      description="Report a hostel problem and follow its progress."
      actions={
        <button
          type="button"
          className="primary-button"
          onClick={openForm}
        >
          + New complaint
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
              <strong>{metrics.total}</strong>
              <small>All submitted complaints</small>
            </article>

            <article>
              <span>Open</span>
              <strong>{metrics.open}</strong>
              <small>Waiting for staff</small>
            </article>

            <article>
              <span>In progress</span>
              <strong>{metrics.inProgress}</strong>
              <small>Being handled</small>
            </article>

            <article className="metric-accent">
              <span>Resolved</span>
              <strong>{metrics.resolved}</strong>
              <small>Completed</small>
            </article>
          </section>

          {showForm && (
            <ComplaintForm
              onCancel={() => setShowForm(false)}
              onSaved={handleSaved}
            />
          )}

          {unreadNotifications.length > 0 && (
            <section className="admin-users-card leave-alerts">
              <div className="admin-users-toolbar">
                <div>
                  <h2>Updates</h2>
                  <span>
                    {unreadNotifications.length} unread
                  </span>
                </div>
              </div>

              <ul className="leave-alert-list">
                {unreadNotifications.slice(0, 5).map((item) => (
                  <li
                    key={item.notificationId}
                    className="leave-alert"
                  >
                    <div>
                      <p>{item.message}</p>
                      <small>
                        {formatDateTime(item.createdAt)}
                      </small>
                    </div>
                    <button
                      type="button"
                      className="secondary-button"
                      onClick={() => handleMarkRead(item)}
                    >
                      Mark as read
                    </button>
                  </li>
                ))}
              </ul>
            </section>
          )}

          <section className="admin-users-card">
            <div className="admin-users-toolbar">
              <div>
                <h2>Complaint history</h2>
                <span>
                  {complaints.length} complaint
                  {complaints.length === 1 ? "" : "s"}
                  {lastUpdated
                    ? ` · updated ${formatDateTime(lastUpdated)}`
                    : ""}
                </span>
              </div>

              <button
                type="button"
                className="secondary-button"
                disabled={isLoading}
                onClick={() => loadData(false)}
              >
                Refresh
              </button>
            </div>

            {isLoading ? (
              <div className="admin-table-state">
                <div className="loading-spinner" />
                <p>Loading your complaints...</p>
              </div>
            ) : complaints.length === 0 ? (
              <div className="admin-table-state">
                <h3>No complaints yet</h3>
                <p>
                  Select “New complaint” to report a
                  problem.
                </p>
              </div>
            ) : (
              <div className="admin-table-wrapper">
                <table className="admin-users-table leave-table complaint-table">
                  <thead>
                    <tr>
                      <th>Complaint</th>
                      <th>Category</th>
                      <th>Description</th>
                      <th>Status</th>
                      <th>Progress</th>
                    </tr>
                  </thead>

                  <tbody>
                    {complaints.map((item) => (
                      <tr key={item.complaintId}>
                        <td>
                          <div className="room-block-cell">
                            <strong>
                              #{item.complaintId}
                            </strong>
                            <small>
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
                            {item.assignedAt ? (
                              <small>
                                Assigned{" "}
                                {formatDateTime(item.assignedAt)}
                              </small>
                            ) : (
                              <small>Not assigned yet</small>
                            )}

                            {item.resolvedAt && (
                              <small>
                                Resolved{" "}
                                {formatDateTime(item.resolvedAt)}
                              </small>
                            )}
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

export default StudentComplaintsPage;