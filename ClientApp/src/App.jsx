import { Navigate, Route, Routes } from 'react-router-dom';
import NavigationBar from './components/NavigationBar.jsx';
import { RequireAdmin, RequireAuth } from './components/RequireAuth.jsx';
import AdminScreeningCreatePage from './pages/AdminScreeningCreatePage.jsx';
import AdminUserEditPage from './pages/AdminUserEditPage.jsx';
import AdminUsersPage from './pages/AdminUsersPage.jsx';
import LoginPage from './pages/LoginPage.jsx';
import ProfilePage from './pages/ProfilePage.jsx';
import RegisterPage from './pages/RegisterPage.jsx';
import ScreeningDetailsPage from './pages/ScreeningDetailsPage.jsx';
import ScreeningsPage from './pages/ScreeningsPage.jsx';

export default function App() {
  return (
    <div className="app-frame">
      <NavigationBar />
      <main className="app-main container">
        <Routes>
          <Route path="/" element={<Navigate to="/screenings" replace />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/screenings" element={<ScreeningsPage />} />
          <Route path="/screenings/:id" element={<ScreeningDetailsPage />} />
          <Route
            path="/profile"
            element={
              <RequireAuth>
                <ProfilePage />
              </RequireAuth>
            }
          />
          <Route
            path="/admin/users"
            element={
              <RequireAdmin>
                <AdminUsersPage />
              </RequireAdmin>
            }
          />
          <Route
            path="/admin/users/:id"
            element={
              <RequireAdmin>
                <AdminUserEditPage />
              </RequireAdmin>
            }
          />
          <Route
            path="/admin/screenings/create"
            element={
              <RequireAdmin>
                <AdminScreeningCreatePage />
              </RequireAdmin>
            }
          />
          <Route path="*" element={<Navigate to="/screenings" replace />} />
        </Routes>
      </main>
      <footer className="app-footer">
        <div className="container">Cinema Ticket Reservation System</div>
      </footer>
    </div>
  );
}
