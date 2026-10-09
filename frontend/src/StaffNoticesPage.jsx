import AppShell from "./AppShell.jsx";

import { useEffect, useMemo, useState } from "react";

import NoticeForm from "./NoticeForm.jsx";

import {
  deleteNotice,
  getActiveBlocks,
  getNotices,
  NoticeApiError
} from "./noticeApi.js";

import { noticeTypeLabels } from "./noticeFormat.js";
import { formatDate } from "./leaveFormat.js";

const staffRoles = ["WARDEN", "HOSTEL_MASTER", "ADMIN"];

function StaffNoticesPage() {
  const [notices, setNotices] = useState([]);
  const [blocks, setBlocks] = useState([]);
  const [blocksFailed, setBlocksFailed] = useState(false);
  const [includeArchived, setIncludeArchived] = useState(false);
  const [searchText, setSearchText] = useState("");
  const [panelMode, setPanelMode] = useState(null);
  const [selected, setSelected] = useState(null);
  const [toDelete, setToDelete] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isDeleting, setIsDeleting] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  useEffect(() => {
    const token = sessionStorage.getItem("accessToken");
    const role = sessionStorage.getItem("userRole");

    if (!token || !staffRoles.includes(role)) {
      window.location.replace("/");
      return;
    }

    getActiveBlocks()
      .then(setBlocks)
      .catch(() => setBlocksFailed(true));
  }, []);

  useEffect(() => {
    loadNotices(includeArchived);
  }, [includeArchived]);

  async function loadNotices(withArchived) {
    setIsLoading(true);
    setErrorMessage("");

    try {
      setNotices(await getNotices(withArchived));
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

  const blockLabels = useMemo(
    () => new Map(blocks.map((b) => [b.blockId, b.blockCode])),
    [blocks]
  );

  function blockLabel(id) {
    if (id == null) return "All blocks";
    return blockLabels.get(id) ?? `Block #${id}`;
  }

  const filtered = useMemo(() => {
    const query = searchText.trim().toLowerCase();
    if (!query) return notices;

    return notices.filter((n) =>
      [n.title, n.content, noticeTypeLabels[n.noticeType]]
        .filter(Boolean)
        .some((v) => v.toLowerCase().includes(query))
    );
  }, [notices, searchText]);

  function closePanel() {
    setSelected(null);
    setPanelMode(null);
  }

  function openCreate() {
    setSelected(null);
    setPanelMode("create");
    setSuccessMessage("");
    setErrorMessage("");
  }

  function openEdit(notice) {
    setSelected(notice);
    setPanelMode("edit");
    setSuccessMessage("");
    setErrorMessage("");
  }

  async function handleSaved(saved, message) {
    closePanel();
    setSuccessMessage(message);
    await loadNotices(includeArchived);
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  async function confirmDelete() {
    if (!toDelete) return;

    setIsDeleting(true);
    setErrorMessage("");

    try {
      await deleteNotice(toDelete.noticeId);
      setNotices((current) =>
        current.filter((n) => n.noticeId !== toDelete.noticeId));
      setSuccessMessage(`"${toDelete.title}" was deleted.`);
    } catch (error) {
      setErrorMessage(
        error instanceof NoticeApiError && error.status !== 401
          ? error.message
          : "Unable to delete the notice. Please try again."
      );
    } finally {
      setToDelete(null);
      setIsDeleting(false);
    }
  }

  return (
    <AppShell
      activePage="notices"
      eyebrow="NOTICES & SCHEDULES"
      title="Notices and schedules"
      description="Publish notices and timetables for a hostel block or for everyone."
      actions={
        <button type="button" className="primary-button" onClick={openCreate}>
          + New notice
        </button>
      }
    >
      <div className="admin-users-page leave-page">
        <section className="admin-users-content">
          {successMessage && (
            <div className="message success" role="status">{successMessage}</div>
          )}
          {errorMessage && (
            <div className="message error" role="alert">{errorMessage}</div>
          )}

          {panelMode === "create" && (
            <NoticeForm
              key="create-notice"
              blocks={blocks}
              blocksFailed={blocksFailed}
              onCancel={closePanel}
              onSaved={handleSaved}
            />
          )}

          {panelMode === "edit" && selected && (
            <NoticeForm
              key={`edit-${selected.noticeId}`}
              notice={selected}
              blocks={blocks}
              blocksFailed={blocksFailed}
              onCancel={closePanel}
              onSaved={handleSaved}
            />
          )}

          <section className="admin-users-card">
            <div className="admin-users-toolbar">
              <div>
                <h2>{includeArchived ? "All notices" : "Active notices"}</h2>
                <span>
                  {filtered.length} item{filtered.length === 1 ? "" : "s"}
                </span>
              </div>

              <div className="notice-toolbar-controls">
                <label className="notice-toggle">
                  <input
                    type="checkbox"
                    checked={includeArchived}
                    onChange={(e) => setIncludeArchived(e.target.checked)}
                  />
                  Show archived
                </label>

                <div className="admin-search">
                  <label htmlFor="noticeSearch" className="sr-only">
                    Search notices
                  </label>
                  <input
                    id="noticeSearch"
                    type="search"
                    value={searchText}
                    onChange={(e) => setSearchText(e.target.value)}
                    placeholder="Search title or content"
                  />
                </div>
              </div>
            </div>

            {isLoading ? (
              <div className="admin-table-state">
                <div className="loading-spinner" />
                <p>Loading notices...</p>
              </div>
            ) : filtered.length === 0 ? (
              <div className="admin-table-state">
                <h3>No notices found</h3>
                <p>Publish a notice or change your search.</p>
              </div>
            ) : (
              <div className="admin-table-wrapper">
                <table className="admin-users-table notice-table">
                  <thead>
                    <tr>
                      <th>Notice</th>
                      <th>Type</th>
                      <th>Audience</th>
                      <th>Expires</th>
                      <th>Status</th>
                      <th><span className="sr-only">Actions</span></th>
                    </tr>
                  </thead>

                  <tbody>
                    {filtered.map((n) => (
                      <tr key={n.noticeId}>
                        <td className="notice-content-cell">
                          <div className="room-block-cell">
                            <strong>{n.title}</strong>
                            <small>{n.content.length > 90
                              ? `${n.content.slice(0, 90)}…`
                              : n.content}</small>
                          </div>
                        </td>

                        <td>
                          <span className={`notice-type ${n.noticeType.toLowerCase()}`}>
                            {noticeTypeLabels[n.noticeType] ?? n.noticeType}
                          </span>
                        </td>

                        <td>{blockLabel(n.hostelBlockId)}</td>
                        <td>{formatDate(n.expiryDate)}</td>

                        <td>
                          <span className={`notice-status ${n.isArchived ? "archived" : "active"}`}>
                            {n.isArchived ? "Archived" : "Active"}
                          </span>
                        </td>

                        <td>
                          <div className="admin-row-actions">
                            <button
                              type="button"
                              disabled={n.isArchived}
                              onClick={() => openEdit(n)}
                            >
                              Edit
                            </button>
                            <button
                              type="button"
                              className="deactivate-button"
                              onClick={() => setToDelete(n)}
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
      </div>

      {toDelete && (
        <div
          className="admin-dialog-backdrop"
          role="presentation"
          onMouseDown={(e) => {
            if (e.target === e.currentTarget && !isDeleting) setToDelete(null);
          }}
        >
          <section
            className="admin-confirm-dialog"
            role="alertdialog"
            aria-modal="true"
            aria-labelledby="delete-notice-title"
          >
            <div className="admin-warning-icon">!</div>
            <h2 id="delete-notice-title">Delete notice?</h2>
            <p>
              <strong>{toDelete.title}</strong> will be removed for every
              student. This cannot be undone.
            </p>

            <div className="admin-dialog-actions">
              <button
                type="button"
                className="secondary-button"
                disabled={isDeleting}
                onClick={() => setToDelete(null)}
              >
                Cancel
              </button>
              <button
                type="button"
                className="danger-button"
                disabled={isDeleting}
                onClick={confirmDelete}
              >
                {isDeleting ? "Deleting..." : "Delete notice"}
              </button>
            </div>
          </section>
        </div>
      )}
    </AppShell>
  );
}

export default StaffNoticesPage;