const roleTitles = {
  STUDENT: "Student Dashboard",
  WARDEN: "Warden Dashboard",
  HOSTEL_MASTER: "Hostel Master Dashboard",
  ADMIN: "Administrator Dashboard"
};

function RoleLandingPage({ requiredRole }) {
  const storedRole = sessionStorage.getItem("userRole");
  const accessToken = sessionStorage.getItem("accessToken");
  const username = sessionStorage.getItem("username");

  if (!accessToken || storedRole !== requiredRole) {
    window.location.replace("/");
    return null;
  }

  function handleLogout() {
    sessionStorage.removeItem("accessToken");
    sessionStorage.removeItem("userRole");
    sessionStorage.removeItem("username");

    window.location.replace("/");
  }

  return (
    <main className="dashboard-page">
      <section className="dashboard-card">
        <p className="dashboard-label">
          Hostel Management System
        </p>

        <h1>{roleTitles[requiredRole]}</h1>

        <p>
          Welcome, <strong>{username}</strong>. You have
          successfully signed in with the{" "}
          <strong>{requiredRole}</strong> role.
        </p>

        <button type="button" onClick={handleLogout}>
          Sign out
        </button>
      </section>
    </main>
  );
}

export default RoleLandingPage;