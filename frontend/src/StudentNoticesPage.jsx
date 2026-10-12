import AppShell from "./AppShell.jsx";

import { useEffect, useMemo, useState } from "react";

import { getMyNotices, NoticeApiError } from "./noticeApi.js";
import { getOwnBlockId, noticeTypeLabels } from "./noticeFormat.js";
import { formatDate, formatDateTime } from "./leaveFormat.js";

const REFRESH_INTERVAL_MS = 60000;

const tabs = [
  { value: "ALL", label: "All" },
  { value: "NOTICE", label: "Notices" },
  { value: "SCHEDULE", label: "Schedules" }
];

function StudentNoticesPage() {
  const [notices, setNotices] = useState([]);
  const [tab, setTab] = useState("ALL");
  const [isLoading, setIsLoading] = useState(true);
  const [lastUpdated, setLastUpdated] = useState(null);
  const [errorMessage, setErrorMessage] = useState("");

  const blockId = getOwnBlockId();

  useEffect(() => {
    const token = sessionStorage.getItem("accessToken");
    const role = sessionStorage.getItem("userRole");

    if (!token || role !== "STUDENT") {
      window.location.replace("/");
      return undefined;
    }

    load(false);
    const timer = window.setInterval(() => load(true), REFRESH_INTERVAL_MS);
    return () => window.clearInterval(timer);
  }, []);

  async function load(isBackgroundRefresh) {
    if (!isBackgroundRefresh) setIsLoading(true);

    try {
      setNotices(await getMyNotices());
      setLastUpdated(new Date());
      setErrorMessage("");
    } catch (error) {
      setErrorMessage(
        error instanceof NoticeApiError && error.status !== 401
          ? error.message
          : "Unable to load notices."
      );
    } finally {
      setIsLoading(false);
    }
  }

  const visible = useMemo(
    () => tab === "ALL" ? notices : notices.filter((n) => n.noticeType === tab),
    [notices, tab]
  );

  return (
    <AppShell
      activePage="notices"
      eyebrow="NOTICES & SCHEDULES"
      title="Notices and schedules"
      description="Current announcements for your hostel block and for everyone."
    >
      <div className="admin-users-page leave-page">
        <section className="admin-users-content">
          {errorMessage && (
            <div className="message error" role="alert">{errorMessage}</div>
          )}

          <div className="notice-banner">
            {blockId
              ? "Showing notices for your hostel block and general notices."
              : "You are not allocated to a hostel block yet, so only general notices are shown. If you were allocated recently, sign in again to refresh this."}
          </div>

          <section className="admin-users-card">
            <div className="admin-users-toolbar">
              <div>
                <h2>Active notices</h2>
                <span>
                  {visible.length} item{visible.length === 1 ? "" : "s"}
                  {lastUpdated ? ` · updated ${formatDateTime(lastUpdated)}` : ""}
                </span>
              </div>

              <div className="notice-tabs" role="tablist">
                {tabs.map((t) => (
                  <button
                    key={t.value}
                    type="button"
                    role="tab"
                    aria-selected={tab === t.value}
                    className={tab === t.value ? "active" : ""}
                    onClick={() => setTab(t.value)}
                  >
                    {t.label}
                  </button>
                ))}
              </div>
            </div>

            {isLoading ? (
              <div className="admin-table-state">
                <div className="loading-spinner" />
                <p>Loading notices...</p>
              </div>
            ) : visible.length === 0 ? (
              <div className="admin-table-state">
                <h3>Nothing to show</h3>
                <p>There are no active notices in this category.</p>
              </div>
            ) : (
              <div className="notice-grid">
                {visible.map((n) => (
                  <article key={n.noticeId} className="notice-card">
                    <div className="notice-card-header">
                      <span className={`notice-type ${n.noticeType.toLowerCase()}`}>
                        {noticeTypeLabels[n.noticeType] ?? n.noticeType}
                      </span>
                      <span className="notice-audience">
                        {n.hostelBlockId == null ? "All blocks" : "Your block"}
                      </span>
                    </div>

                    <h3>{n.title}</h3>
                    <p>{n.content}</p>

                    <small>
                      Posted {formatDateTime(n.createdAt)} · valid until{" "}
                      {formatDate(n.expiryDate)}
                    </small>
                  </article>
                ))}
              </div>
            )}
          </section>
        </section>
      </div>
    </AppShell>
  );
}

export default StudentNoticesPage;