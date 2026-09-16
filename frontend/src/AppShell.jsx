import { useState } from "react";

const navigationByRole = {
  ADMIN: [
    {
      key: "dashboard",
      label: "Dashboard",
      href: "/admin",
      icon: "dashboard"
    },
    {
      key: "users",
      label: "Users",
      href: "/admin/users",
      icon: "users"
    },
    {
      key: "rooms",
      label: "Rooms",
      href: "/admin/rooms",
      icon: "rooms"
    },
    {
      key: "allocations",
      label: "Allocations",
      href: "/admin/allocations",
      icon: "allocations"
    }
  ],
  STUDENT: [
    {
      key: "dashboard",
      label: "Dashboard",
      href: "/student",
      icon: "dashboard"
    },
    {
      key: "profile",
      label: "My profile",
      href: "/student/profile",
      icon: "profile"
    }
  ],
  WARDEN: [
    {
      key: "dashboard",
      label: "Dashboard",
      href: "/warden",
      icon: "dashboard"
    }
  ],
  HOSTEL_MASTER: [
    {
      key: "dashboard",
      label: "Dashboard",
      href: "/hostel-master",
      icon: "dashboard"
    }
  ]
};

const roleLabels = {
  ADMIN: "Administrator",
  STUDENT: "Student",
  WARDEN: "Warden",
  HOSTEL_MASTER: "Hostel Master"
};

function NavIcon({ name }) {
  const icons = {
    dashboard: (
      <>
        <rect x="3" y="3" width="7" height="7" rx="1" />
        <rect x="14" y="3" width="7" height="7" rx="1" />
        <rect x="3" y="14" width="7" height="7" rx="1" />
        <rect x="14" y="14" width="7" height="7" rx="1" />
      </>
    ),
    users: (
      <>
        <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" />
        <circle cx="9" cy="7" r="4" />
        <path d="M22 21v-2a4 4 0 0 0-3-3.87" />
        <path d="M16 3.13a4 4 0 0 1 0 7.75" />
      </>
    ),
    rooms: (
      <>
        <path d="M3 21V5a2 2 0 0 1 2-2h11a2 2 0 0 1 2 2v16" />
        <path d="M3 21h18" />
        <path d="M14 9h.01" />
      </>
    ),
    allocations: (
      <>
        <rect x="4" y="4" width="16" height="17" rx="2" />
        <path d="M9 4V2h6v2" />
        <path d="M8 11h8" />
        <path d="M8 15h5" />
      </>
    ),
    profile: (
      <>
        <circle cx="12" cy="8" r="4" />
        <path d="M4 21a8 8 0 0 1 16 0" />
      </>
    )
  };

  return (
    <svg
      className="app-nav-icon"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      {icons[name]}
    </svg>
  );
}

function AppShell({
  activePage,
  eyebrow,
  title,
  description,
  actions,
  children
}) {
  const [isMenuOpen, setIsMenuOpen] = useState(false);

  const role =
    sessionStorage.getItem("userRole") ?? "";

  const username =
    sessionStorage.getItem("username") ?? "User";

  const navigation = navigationByRole[role] ?? [];

  function handleLogout() {
    sessionStorage.clear();
    window.location.replace("/");
  }

  return (
    <div className="app-shell">
      <aside
        className={
          isMenuOpen
            ? "app-sidebar menu-open"
            : "app-sidebar"
        }
      >
        <div className="app-brand">
          <span className="app-brand-mark">H</span>

          <div>
            <strong>HMS</strong>
            <small>Hostel Management</small>
          </div>
        </div>

        <nav className="app-navigation">
          <span className="app-navigation-label">
            MAIN MENU
          </span>

          {navigation.map((item) => (
            <a
              key={item.key}
              href={item.href}
              className={
                activePage === item.key
                  ? "app-nav-link active"
                  : "app-nav-link"
              }
            >
              <NavIcon name={item.icon} />
              <span>{item.label}</span>
            </a>
          ))}
        </nav>

        <div className="app-sidebar-footer">
          <div className="app-sidebar-user">
            <span>
              {username.charAt(0).toUpperCase()}
            </span>

            <div>
              <strong>{username}</strong>
              <small>{roleLabels[role] ?? role}</small>
            </div>
          </div>

          <button
            type="button"
            className="app-sign-out"
            onClick={handleLogout}
          >
            Sign out
          </button>
        </div>
      </aside>

      {isMenuOpen && (
        <button
          type="button"
          className="app-sidebar-overlay"
          aria-label="Close navigation"
          onClick={() => setIsMenuOpen(false)}
        />
      )}

      <section className="app-workspace">
        <header className="app-topbar">
          <button
            type="button"
            className="app-menu-button"
            aria-label="Open navigation"
            onClick={() => setIsMenuOpen(true)}
          >
            <span />
            <span />
            <span />
          </button>

          <div className="app-topbar-context">
            <span>Hostel Management System</span>
            <strong>{roleLabels[role] ?? role}</strong>
          </div>

          <div className="app-user-avatar">
            {username.charAt(0).toUpperCase()}
          </div>
        </header>

        <main className="app-main">
          <div className="app-page-heading">
            <div>
              {eyebrow && <p>{eyebrow}</p>}
              <h1>{title}</h1>
              {description && <span>{description}</span>}
            </div>

            {actions && (
              <div className="app-page-actions">
                {actions}
              </div>
            )}
          </div>

          {children}
        </main>
      </section>
    </div>
  );
}

export default AppShell;