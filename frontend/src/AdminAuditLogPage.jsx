import AppShell from "./AppShell.jsx";

import { useEffect, useMemo, useState } from "react";

import {
  ToolsApiError,
  getLoginAuditLogs
} from "./adminToolsApi.js";
import { formatDateTime } from "./leaveFormat.js";

const LIMIT_OPTIONS = [50, 100, 250, 500];

function AdminAuditLogPage() {
  const [logs, setLogs] = useState([]);
  const [limit, setLimit] = useState(100);
  const [outcomeFilter, setOutcomeFilter] = useState("");
  const [searchText, setSearchText] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    const token = sessionStorage.getItem("accessToken");
    const role = sessionStorage.getItem("userRole");

    if (!token || role !== "ADMIN") {
      window.location.replace("/");
      return;
    }

    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [limit]);

  async function load() {
    setIsLoading(true);

    try {
      setLogs(await getLoginAuditLogs(limit));
      setErrorMessage("");
    } catch (error) {
      setErrorMessage(
        error instanceof ToolsApiError && error.status !== 401
          ? error.message
          : "Unable to load the login audit log."
      );
    } finally {
      setIsLoading(false);
    }
  }

  const visibleLogs = useMemo(() => {
    const search = searchText.trim().toLowerCase();

    return logs.filter(
      (log) =>
        (!outcomeFilter || log.outcome === outcomeFilter) &&
        (!search ||
          log.identifier.toLowerCase().includes(search))
    );
  }, [logs, outcomeFilter, searchText]);

  const failureCount = visibleLogs.filter(
    (log) => log.outcome === "FAILURE"
  ).length;

  return (
    <AppShell
      activePage="audit"
      eyebrow="SECURITY"
      title="Login audit log"
      description="Recent sign-in attempts, including failed attempts that can lead to account lockouts."
      actions={
        <button
          type="button"
          className="secondary-button"
          disabled={isLoading}
          onClick={load}
        >
          Refresh
        </button>
      }
    >
      <div className="admin-users-page">
        <section className="admin-users-content">
          {errorMessage && (
            <div className="message error" role="alert">
              {errorMessage}
            </div>
          )}

          <section className="admin-users-card">
            <div className="admin-users-toolbar">
              <div>
                <h2>Sign-in attempts</h2>

                <span>
                  {visibleLogs.length} shown · {failureCount}{" "}
                  failed
                </span>
              </div>

              <div className="tools-filters">
                <div className="form-group">
                  <label htmlFor="auditSearch">
                    Email or username
                  </label>

                  <input
                    id="auditSearch"
                    type="search"
                    value={searchText}
                    onChange={(event) =>
                      setSearchText(event.target.value)
                    }
                    placeholder="Search identifier"
                  />
                </div>

                <div className="form-group">
                  <label htmlFor="auditOutcome">Outcome</label>

                  <select
                    id="auditOutcome"
                    value={outcomeFilter}
                    onChange={(event) =>
                      setOutcomeFilter(event.target.value)
                    }
                  >
                    <option value="">All</option>
                    <option value="SUCCESS">Success</option>
                    <option value="FAILURE">Failure</option>
                  </select>
                </div>

                <div className="form-group">
                  <label htmlFor="auditLimit">Load latest</label>

                  <select
                    id="auditLimit"
                    value={limit}
                    onChange={(event) =>
                      setLimit(Number(event.target.value))
                    }
                  >
                    {LIMIT_OPTIONS.map((option) => (
                      <option key={option} value={option}>
                        {option} attempts
                      </option>
                    ))}
                  </select>
                </div>
              </div>
            </div>

            {isLoading ? (
              <div className="admin-table-state">
                <div className="loading-spinner" />
                <p>Loading audit log...</p>
              </div>
            ) : visibleLogs.length === 0 ? (
              <div className="admin-table-state">
                <h3>No attempts found</h3>
                <p>Try changing the filters.</p>
              </div>
            ) : (
              <div className="admin-table-wrapper">
                <table className="admin-users-table">
                  <thead>
                    <tr>
                      <th>Time</th>
                      <th>Email or username</th>
                      <th>User ID</th>
                      <th>Outcome</th>
                    </tr>
                  </thead>

                  <tbody>
                    {visibleLogs.map((log) => (
                      <tr key={log.auditLogId}>
                        <td>{formatDateTime(log.attemptedAt)}</td>
                        <td>{log.identifier}</td>
                        <td>{log.userId ?? "Unknown account"}</td>
                        <td>
                          <span
                            className={`status-badge ${log.outcome.toLowerCase()}`}
                          >
                            {log.outcome === "SUCCESS"
                              ? "Success"
                              : "Failure"}
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

export default AdminAuditLogPage;
