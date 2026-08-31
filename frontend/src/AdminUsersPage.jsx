import {
  useEffect,
  useMemo,
  useState
} from "react";

import AdminUserForm from
  "./AdminUserForm.jsx";

import {
  AdminApiError,
  deactivateAdminUser,
  getAdminUsers
} from "./adminUserApi.js";

function formatDate(value) {
  if (!value) {
    return "Not provided";
  }

  return new Intl.DateTimeFormat("en-GB", {
    year: "numeric",
    month: "short",
    day: "numeric"
  }).format(new Date(value));
}

function displayValue(value) {
  return value || "Not provided";
}

function UserDetail({
  user,
  onEdit,
  onClose
}) {
  return (
    <section className="admin-detail-panel">
      <div className="admin-panel-heading">
        <div>
          <p>ACCOUNT DETAILS</p>
          <h2>
            {user.firstName} {user.lastName}
          </h2>
          <span>
            View the selected user account information.
          </span>
        </div>

        <button
          type="button"
          className="secondary-button"
          onClick={onClose}
        >
          Close
        </button>
      </div>

      <div className="admin-detail-grid">
        <div>
          <span>User ID</span>
          <strong>{user.userId}</strong>
        </div>

        <div>
          <span>Username</span>
          <strong>{user.username}</strong>
        </div>

        <div>
          <span>Email address</span>
          <strong>{user.email}</strong>
        </div>

        <div>
          <span>Phone number</span>
          <strong>
            {displayValue(user.phoneNumber)}
          </strong>
        </div>

        <div>
          <span>Role</span>
          <strong>{user.roleName}</strong>
        </div>

        <div>
          <span>Status</span>

          <strong
            className={
              user.isActive
                ? "status-text active"
                : "status-text inactive"
            }
          >
            {user.isActive
              ? "Active"
              : "Inactive"}
          </strong>
        </div>

        <div>
          <span>Created</span>
          <strong>
            {formatDate(user.createdAt)}
          </strong>
        </div>

        <div>
          <span>Last updated</span>
          <strong>
            {formatDate(user.updatedAt)}
          </strong>
        </div>
      </div>

      <div className="admin-form-actions">
        <button
          type="button"
          className="primary-button"
          onClick={() => onEdit(user)}
        >
          Edit account
        </button>
      </div>
    </section>
  );
}

function AdminUsersPage() {
  const [users, setUsers] = useState([]);
  const [searchText, setSearchText] =
    useState("");

  const [panelMode, setPanelMode] =
    useState(null);

  const [selectedUser, setSelectedUser] =
    useState(null);

  const [userToDeactivate, setUserToDeactivate] =
    useState(null);

  const [isLoading, setIsLoading] =
    useState(true);

  const [isDeactivating, setIsDeactivating] =
    useState(false);

  const [errorMessage, setErrorMessage] =
    useState("");

  const [successMessage, setSuccessMessage] =
    useState("");

  useEffect(() => {
    const accessToken =
      sessionStorage.getItem("accessToken");

    const role =
      sessionStorage.getItem("userRole");

    if (!accessToken || role !== "ADMIN") {
      window.location.replace("/");
      return;
    }

    loadUsers();
  }, []);

  async function loadUsers() {
    setIsLoading(true);
    setErrorMessage("");

    try {
      const data = await getAdminUsers();
      setUsers(data);
    } catch (error) {
      if (
        error instanceof AdminApiError &&
        error.status !== 401
      ) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage(
          "Unable to load user accounts."
        );
      }
    } finally {
      setIsLoading(false);
    }
  }

  const filteredUsers = useMemo(() => {
    const query =
      searchText.trim().toLowerCase();

    if (!query) {
      return users;
    }

    return users.filter((user) =>
      [
        user.username,
        user.email,
        user.firstName,
        user.lastName,
        user.phoneNumber,
        user.roleName
      ]
        .filter(Boolean)
        .some((value) =>
          value.toLowerCase().includes(query)
        )
    );
  }, [users, searchText]);

  function openCreateForm() {
    setSelectedUser(null);
    setPanelMode("create");
    setSuccessMessage("");
    setErrorMessage("");
  }

  function openUserDetails(user) {
    setSelectedUser(user);
    setPanelMode("view");
    setSuccessMessage("");
  }

  function openEditForm(user) {
    setSelectedUser(user);
    setPanelMode("edit");
    setSuccessMessage("");
    setErrorMessage("");
  }

  function closePanel() {
    setSelectedUser(null);
    setPanelMode(null);
  }

  function handleUserSaved(
    savedUser,
    message
  ) {
    setUsers((currentUsers) => {
      const exists = currentUsers.some(
        (user) =>
          user.userId === savedUser.userId
      );

      if (exists) {
        return currentUsers.map((user) =>
          user.userId === savedUser.userId
            ? savedUser
            : user
        );
      }

      return [savedUser, ...currentUsers];
    });

    setPanelMode(null);
    setSelectedUser(null);
    setSuccessMessage(message);

    window.scrollTo({
      top: 0,
      behavior: "smooth"
    });
  }

  async function confirmDeactivation() {
    if (!userToDeactivate) {
      return;
    }

    setIsDeactivating(true);
    setErrorMessage("");

    try {
      await deactivateAdminUser(
        userToDeactivate.userId
      );

      setUsers((currentUsers) =>
        currentUsers.map((user) =>
          user.userId ===
          userToDeactivate.userId
            ? {
                ...user,
                isActive: false
              }
            : user
        )
      );

      setSuccessMessage(
        `${userToDeactivate.username} was deactivated successfully.`
      );

      setUserToDeactivate(null);
      closePanel();
    } catch (error) {
      if (
        error instanceof AdminApiError &&
        error.status !== 401
      ) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage(
          "Unable to deactivate the user account."
        );
      }

      setUserToDeactivate(null);
    } finally {
      setIsDeactivating(false);
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
            <strong>
              Hostel Management System
            </strong>
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
            <p>USER MANAGEMENT</p>
            <h1>User accounts</h1>

            <span>
              Create, view, update and deactivate
              system user accounts.
            </span>
          </div>

          <button
            type="button"
            className="primary-button"
            onClick={openCreateForm}
          >
            + Create user
          </button>
        </div>

        {successMessage && (
          <div
            className="message success"
            role="status"
          >
            {successMessage}
          </div>
        )}

        {errorMessage && (
          <div
            className="message error"
            role="alert"
          >
            {errorMessage}
          </div>
        )}

        {panelMode === "create" && (
          <AdminUserForm
            key="create-user"
            onCancel={closePanel}
            onSaved={handleUserSaved}
          />
        )}

        {panelMode === "edit" &&
          selectedUser && (
            <AdminUserForm
              key={`edit-${selectedUser.userId}`}
              user={selectedUser}
              onCancel={closePanel}
              onSaved={handleUserSaved}
            />
          )}

        {panelMode === "view" &&
          selectedUser && (
            <UserDetail
              user={selectedUser}
              onEdit={openEditForm}
              onClose={closePanel}
            />
          )}

        <section className="admin-users-card">
          <div className="admin-users-toolbar">
            <div>
              <h2>All users</h2>

              <span>
                {filteredUsers.length} account
                {filteredUsers.length === 1
                  ? ""
                  : "s"}
              </span>
            </div>

            <div className="admin-search">
              <label
                htmlFor="adminUserSearch"
                className="sr-only"
              >
                Search users
              </label>

              <input
                id="adminUserSearch"
                type="search"
                value={searchText}
                onChange={(event) =>
                  setSearchText(
                    event.target.value
                  )
                }
                placeholder="Search name, email or role"
              />
            </div>
          </div>

          {isLoading ? (
            <div className="admin-table-state">
              <div className="loading-spinner" />
              <p>Loading user accounts...</p>
            </div>
          ) : filteredUsers.length === 0 ? (
            <div className="admin-table-state">
              <h3>No users found</h3>
              <p>
                Try changing your search or create
                a new account.
              </p>
            </div>
          ) : (
            <div className="admin-table-wrapper">
              <table className="admin-users-table">
                <thead>
                  <tr>
                    <th>User</th>
                    <th>Username</th>
                    <th>Role</th>
                    <th>Status</th>
                    <th>Created</th>
                    <th>
                      <span className="sr-only">
                        Actions
                      </span>
                    </th>
                  </tr>
                </thead>

                <tbody>
                  {filteredUsers.map((user) => (
                    <tr key={user.userId}>
                      <td>
                        <div className="admin-user-cell">
                          <span>
                            {user.firstName
                              ?.charAt(0)
                              .toUpperCase()}
                            {user.lastName
                              ?.charAt(0)
                              .toUpperCase()}
                          </span>

                          <div>
                            <strong>
                              {user.firstName}{" "}
                              {user.lastName}
                            </strong>

                            <small>{user.email}</small>
                          </div>
                        </div>
                      </td>

                      <td>{user.username}</td>

                      <td>
                        <span className="role-badge">
                          {user.roleName}
                        </span>
                      </td>

                      <td>
                        <span
                          className={
                            user.isActive
                              ? "status-badge active"
                              : "status-badge inactive"
                          }
                        >
                          {user.isActive
                            ? "Active"
                            : "Inactive"}
                        </span>
                      </td>

                      <td>
                        {formatDate(
                          user.createdAt
                        )}
                      </td>

                      <td>
                        <div className="admin-row-actions">
                          <button
                            type="button"
                            onClick={() =>
                              openUserDetails(user)
                            }
                          >
                            View
                          </button>

                          <button
                            type="button"
                            onClick={() =>
                              openEditForm(user)
                            }
                          >
                            Edit
                          </button>

                          <button
                            type="button"
                            className="deactivate-button"
                            disabled={!user.isActive}
                            onClick={() =>
                              setUserToDeactivate(
                                user
                              )
                            }
                          >
                            {user.isActive
                              ? "Deactivate"
                              : "Inactive"}
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

      {userToDeactivate && (
        <div
          className="admin-dialog-backdrop"
          role="presentation"
          onMouseDown={(event) => {
            if (
              event.target ===
              event.currentTarget
            ) {
              setUserToDeactivate(null);
            }
          }}
        >
          <section
            className="admin-confirm-dialog"
            role="alertdialog"
            aria-modal="true"
            aria-labelledby="deactivate-title"
          >
            <div className="admin-warning-icon">
              !
            </div>

            <h2 id="deactivate-title">
              Deactivate user?
            </h2>

            <p>
              <strong>
                {userToDeactivate.username}
              </strong>{" "}
              will no longer be able to sign in.
            </p>

            <div className="admin-dialog-actions">
              <button
                type="button"
                className="secondary-button"
                disabled={isDeactivating}
                onClick={() =>
                  setUserToDeactivate(null)
                }
              >
                Cancel
              </button>

              <button
                type="button"
                className="danger-button"
                disabled={isDeactivating}
                onClick={confirmDeactivation}
              >
                {isDeactivating
                  ? "Deactivating..."
                  : "Deactivate"}
              </button>
            </div>
          </section>
        </div>
      )}
    </main>
  );
}

export default AdminUsersPage;