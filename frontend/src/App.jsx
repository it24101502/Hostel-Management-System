import { useState } from "react";
import RoleLandingPage from "./RoleLandingPage.jsx";
import StudentProfilePage from
  "./StudentProfilePage.jsx";
import hostelBackground from
  "./assets/hostel-night-login-background.png";

const landingPageRoles = {
  "/student": "STUDENT",
  "/warden": "WARDEN",
  "/hostel-master": "HOSTEL_MASTER",
  "/admin": "ADMIN"
};

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5220";

const roleLandingPages = {
  STUDENT: "/student",
  WARDEN: "/warden",
  HOSTEL_MASTER: "/hostel-master",
  ADMIN: "/admin"
};

function LoginPage() {
  const [identifier, setIdentifier] = useState("");
  const [password, setPassword] = useState("");
  const [errors, setErrors] = useState({});
  const [serverMessage, setServerMessage] = useState("");
  const [messageType, setMessageType] = useState("error");
  const [isSubmitting, setIsSubmitting] = useState(false);

  function validateForm() {
    const validationErrors = {};

    if (!identifier.trim()) {
      validationErrors.identifier =
        "Email address or username is required.";
    }

    if (!password) {
      validationErrors.password = "Password is required.";
    }

    setErrors(validationErrors);

    return Object.keys(validationErrors).length === 0;
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setServerMessage("");

    if (!validateForm()) {
      return;
    }

    setIsSubmitting(true);

    try {
      const response = await fetch(
        `${API_BASE_URL}/api/auth/login`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json"
          },
          body: JSON.stringify({
            identifier: identifier.trim(),
            password
          })
        }
      );

      const data = await response.json().catch(() => ({}));

      if (response.status === 423) {
        setMessageType("lockout");
        setServerMessage(
          data.message ??
            "Your account is temporarily locked. Please try again later."
        );
        return;
      }

      if (!response.ok) {
        setMessageType("error");
        setServerMessage(
          data.message ?? "Invalid credentials."
        );
        return;
      }

      const normalizedRole = data.role?.toUpperCase();
      const landingPage = roleLandingPages[normalizedRole];

      if (!landingPage) {
        setMessageType("error");
        setServerMessage(
          "Your account does not have a supported role."
        );
        return;
      }

      sessionStorage.setItem(
        "accessToken",
        data.accessToken
      );
      sessionStorage.setItem("userRole", normalizedRole);
      sessionStorage.setItem("username", data.username);

      window.location.assign(landingPage);
    } catch {
      setMessageType("error");
      setServerMessage(
        "Unable to connect to the server. Please try again."
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
      <main className="login-page">
        <section className="login-shell">
          <aside
            className="login-visual"
            style={{
              backgroundImage: `url(${hostelBackground})`
            }}
          >
            <div className="visual-overlay" />

            <div className="visual-content">
              <div className="visual-brand">
                <span className="visual-logo">HMS</span>

                <div>
                  <strong>Hostel Management System</strong>
                  <span>Secure student accommodation</span>
                </div>
              </div>

              <div className="visual-message">
                <p>WELCOME TO YOUR HOSTEL PORTAL</p>
                <h2>Manage your hostel life securely and easily.</h2>
                <span>
                  Access your profile, accommodation details,
                  requests and other permitted services.
                </span>
              </div>

              <p className="visual-footer">
                Secure access for students, wardens, hostel
                masters and administrators.
              </p>
            </div>
          </aside>

          <section className="login-panel">
            <div className="mobile-brand">
              <span className="mobile-logo">HMS</span>
              <strong>Hostel Management System</strong>
            </div>

            <div className="login-heading">
              <p>WELCOME BACK</p>
              <h1>Sign in to your account</h1>
              <span>
                Enter your registered email address or username
                and password.
              </span>
            </div>

            {serverMessage && (
              <div
                className={`message ${messageType}`}
                role="alert"
              >
                {serverMessage}
              </div>
            )}

            <form onSubmit={handleSubmit} noValidate>
              <div className="form-group">
                <label htmlFor="identifier">
                  Email address or username
                </label>

                <input
                  id="identifier"
                  name="identifier"
                  type="text"
                  autoComplete="username"
                  value={identifier}
                  aria-invalid={Boolean(errors.identifier)}
                  aria-describedby={
                    errors.identifier
                      ? "identifier-error"
                      : undefined
                  }
                  onChange={(event) => {
                    setIdentifier(event.target.value);

                    if (errors.identifier) {
                      setErrors((current) => ({
                        ...current,
                        identifier: ""
                      }));
                    }
                  }}
                  placeholder="Enter your email or username"
                />

                {errors.identifier && (
                  <p
                    id="identifier-error"
                    className="field-error"
                  >
                    {errors.identifier}
                  </p>
                )}
              </div>

              <div className="form-group">
                <label htmlFor="password">Password</label>

                <input
                  id="password"
                  name="password"
                  type="password"
                  autoComplete="current-password"
                  value={password}
                  aria-invalid={Boolean(errors.password)}
                  aria-describedby={
                    errors.password
                      ? "password-error"
                      : undefined
                  }
                  onChange={(event) => {
                    setPassword(event.target.value);

                    if (errors.password) {
                      setErrors((current) => ({
                        ...current,
                        password: ""
                      }));
                    }
                  }}
                  placeholder="Enter your password"
                />

                {errors.password && (
                  <p
                    id="password-error"
                    className="field-error"
                  >
                    {errors.password}
                  </p>
                )}
              </div>

              <button type="submit" disabled={isSubmitting}>
                {isSubmitting ? "Signing in..." : "Sign in"}
              </button>
            </form>

            <p className="security-note">
              Your account will be temporarily locked after five
              consecutive failed login attempts.
            </p>
          </section>
        </section>
      </main>
    );
}

function App() {
  if (
    window.location.pathname ===
    "/student/profile"
  ) {
    return <StudentProfilePage />;
  }

  const requiredRole =
    landingPageRoles[window.location.pathname];

  if (requiredRole) {
    return (
      <RoleLandingPage requiredRole={requiredRole} />
    );
  }

  return <LoginPage />;
}

export default App;