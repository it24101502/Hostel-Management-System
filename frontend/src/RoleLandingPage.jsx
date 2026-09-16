import AppShell from "./AppShell.jsx";

const roleTitles = {
  STUDENT: "Student Dashboard",
  WARDEN: "Warden Dashboard",
  HOSTEL_MASTER: "Hostel Master Dashboard",
  ADMIN: "Administrator Dashboard"
};

const roleLabels = {
  STUDENT: "Student",
  WARDEN: "Warden",
  HOSTEL_MASTER: "Hostel Master",
  ADMIN: "Administrator"
};

const modulesByRole = {
  ADMIN: [
    {
      number: "01",
      title: "User management",
      description:
        "Create, view and maintain system user accounts.",
      href: "/admin/users",
      accent: "indigo"
    },
    {
      number: "02",
      title: "Room management",
      description:
        "Manage hostel blocks, rooms and bed capacity.",
      href: "/admin/rooms",
      accent: "cyan"
    },
    {
      number: "03",
      title: "Allocations",
      description:
        "Allocate students, transfer rooms and monitor occupancy.",
      href: "/admin/allocations",
      accent: "violet"
    }
  ],
  STUDENT: [
    {
      number: "01",
      title: "My profile",
      description:
        "View your account and update permitted information.",
      href: "/student/profile",
      accent: "indigo"
    }
  ],
  WARDEN: [],
  HOSTEL_MASTER: []
};

function RoleLandingPage({ requiredRole }) {
  const storedRole =
    sessionStorage.getItem("userRole");

  const accessToken =
    sessionStorage.getItem("accessToken");

  const username =
    sessionStorage.getItem("username") ?? "User";

  if (!accessToken || storedRole !== requiredRole) {
    window.location.replace("/");
    return null;
  }

  const modules = modulesByRole[requiredRole] ?? [];
  const roleLabel =
    roleLabels[requiredRole] ?? requiredRole;

  return (
    <AppShell
      activePage="dashboard"
      eyebrow="OVERVIEW"
      title={roleTitles[requiredRole]}
      description={
        "Manage your permitted hostel services from one secure workspace."
      }
    >
      <section className="portal-welcome-card">
        <div className="portal-welcome-content">
          <span className="portal-role-pill">
            {roleLabel} workspace
          </span>

          <h2>
            Welcome back,{" "}
            <strong>{username}</strong>
          </h2>

          <p>
            Your hostel services, account tools and daily
            operations are available from this dashboard.
          </p>
        </div>

        <div
          className="portal-welcome-decoration"
          aria-hidden="true"
        >
          HMS
        </div>
      </section>

      <section className="portal-status-grid">
        <article>
          <span>SESSION</span>
          <strong>Authenticated</strong>
          <small>Your secure session is active</small>
        </article>

        <article>
          <span>ACCESS LEVEL</span>
          <strong>{roleLabel}</strong>
          <small>Role permissions verified</small>
        </article>

        <article>
          <span>WORKSPACE</span>
          <strong>Ready</strong>
          <small>Available services can be opened below</small>
        </article>
      </section>

      <div className="portal-section-heading">
        <div>
          <p>QUICK ACCESS</p>
          <h2>Your services</h2>
        </div>

        <span>
          {modules.length} available module
          {modules.length === 1 ? "" : "s"}
        </span>
      </div>

      {modules.length > 0 ? (
        <section className="portal-module-grid">
          {modules.map((module) => (
            <a
              key={module.title}
              href={module.href}
              className={`portal-module-card ${module.accent}`}
            >
              <div className="portal-module-number">
                {module.number}
              </div>

              <div>
                <h3>{module.title}</h3>
                <p>{module.description}</p>
              </div>

              <span className="portal-module-arrow">
                →
              </span>
            </a>
          ))}
        </section>
      ) : (
        <section className="portal-empty-card">
          <span>✓</span>
          <div>
            <h3>Your workspace is ready</h3>
            <p>
              Additional services for the {roleLabel} role
              will appear here when they become available.
            </p>
          </div>
        </section>
      )}
    </AppShell>
  );
}

export default RoleLandingPage;