import AppShell from "./AppShell.jsx";

import { useEffect, useState } from "react";

import { FeeApiError, getMyFeeReminders } from "./feeApi.js";
import { formatDate, formatDateTime } from "./leaveFormat.js";

const REFRESH_INTERVAL_MS = 60000;

function formatAmount(value) {
  return Number(value).toLocaleString("en-US", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  });
}

function StudentFeesPage() {
  const [reminders, setReminders] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [lastUpdated, setLastUpdated] = useState(null);
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    const token = sessionStorage.getItem("accessToken");
    const role = sessionStorage.getItem("userRole");

    if (!token || role !== "STUDENT") {
      window.location.replace("/");
      return undefined;
    }

    load(false);

    const timer = window.setInterval(
      () => load(true),
      REFRESH_INTERVAL_MS
    );

    return () => window.clearInterval(timer);
  }, []);

  async function load(isBackgroundRefresh) {
    if (!isBackgroundRefresh) {
      setIsLoading(true);
    }

    try {
      setReminders(await getMyFeeReminders());
      setLastUpdated(new Date());
      setErrorMessage("");
    } catch (error) {
      setErrorMessage(
        error instanceof FeeApiError && error.status !== 401
          ? error.message
          : "Unable to load fee reminders."
      );
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <AppShell
      activePage="fees"
      eyebrow="FEES"
      title="Fee reminders"
      description="Overdue hostel fees that still need to be paid."
    >
      <div className="admin-users-page leave-page">
        <section className="admin-users-content">
          {errorMessage && (
            <div className="message error" role="alert">
              {errorMessage}
            </div>
          )}

          <section className="admin-users-card leave-alerts">
            <div className="admin-users-toolbar">
              <div>
                <h2>Overdue invoices</h2>
                <span>
                  {reminders.length} reminder
                  {reminders.length === 1 ? "" : "s"}
                  {lastUpdated
                    ? ` · updated ${formatDateTime(lastUpdated)}`
                    : ""}
                </span>
              </div>

              <button
                type="button"
                className="secondary-button"
                disabled={isLoading}
                onClick={() => load(false)}
              >
                Refresh
              </button>
            </div>

            {isLoading ? (
              <div className="admin-table-state">
                <div className="loading-spinner" />
                <p>Loading fee reminders...</p>
              </div>
            ) : reminders.length === 0 ? (
              <div className="admin-table-state">
                <h3>No overdue fees</h3>
                <p>You have no fee reminders right now.</p>
              </div>
            ) : (
              <ul className="leave-alert-list">
                {reminders.map((item) => (
                  <li
                    key={item.reminderId}
                    className="leave-alert overdue"
                  >
                    <div>
                      <strong>
                        {item.feeType} · {item.invoiceNumber}
                      </strong>

                      <p>{item.message}</p>

                      <small>
                        Outstanding{" "}
                        {formatAmount(item.outstandingAmount)}{" "}
                        of {formatAmount(item.totalAmount)}
                        {" · "}due {formatDate(item.dueDate)}
                        {" · "}reminder sent{" "}
                        {formatDateTime(
                          item.sentAt ?? item.triggeredAt
                        )}
                      </small>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </section>
        </section>
      </div>
    </AppShell>
  );
}

export default StudentFeesPage;