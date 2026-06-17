import { Link, NavLink, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext.jsx';

export default function NavigationBar() {
  const navigate = useNavigate();
  const { isAdmin, isAuthenticated, isLoading, signOut, user } = useAuth();

  async function handleLogout() {
    await signOut();
    navigate('/screenings');
  }

  return (
    <nav className="navbar navbar-expand-lg cinema-navbar">
      <div className="container">
        <Link className="navbar-brand fw-semibold" to="/screenings">
          Cinema Tickets
        </Link>
        <button
          className="navbar-toggler"
          type="button"
          data-bs-toggle="collapse"
          data-bs-target="#mainNavigation"
          aria-controls="mainNavigation"
          aria-expanded="false"
          aria-label="Toggle navigation"
        >
          <span className="navbar-toggler-icon" />
        </button>
        <div className="collapse navbar-collapse" id="mainNavigation">
          <ul className="navbar-nav me-auto mb-2 mb-lg-0">
            <li className="nav-item">
              <NavLink className="nav-link" to="/screenings">
                Screenings
              </NavLink>
            </li>
            {isAuthenticated && (
              <li className="nav-item">
                <NavLink className="nav-link" to="/profile">
                  Profile
                </NavLink>
              </li>
            )}
            {isAdmin && (
              <>
                <li className="nav-item">
                  <NavLink className="nav-link" to="/admin/users">
                    Users
                  </NavLink>
                </li>
                <li className="nav-item">
                  <NavLink className="nav-link" to="/admin/screenings/create">
                    Create Screening
                  </NavLink>
                </li>
              </>
            )}
          </ul>
          <div className="d-flex align-items-center gap-2">
            {isLoading ? (
              <span className="navbar-text text-muted">Checking session...</span>
            ) : isAuthenticated ? (
              <>
                <span className="navbar-text text-muted">{user.email}</span>
                <button className="btn btn-outline-secondary btn-sm" type="button" onClick={handleLogout}>
                  Logout
                </button>
              </>
            ) : (
              <>
                <Link className="btn btn-outline-primary btn-sm" to="/login">
                  Login
                </Link>
                <Link className="btn btn-primary btn-sm" to="/register">
                  Register
                </Link>
              </>
            )}
          </div>
        </div>
      </div>
    </nav>
  );
}
