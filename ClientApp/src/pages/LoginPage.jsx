import { useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { ApiError } from '../api/client.js';
import { useAuth } from '../auth/AuthContext.jsx';

export default function LoginPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { signIn } = useAuth();
  const [form, setForm] = useState({ email: '', password: '', rememberMe: false });
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  function updateField(event) {
    const { name, type, checked, value } = event.target;
    setForm((current) => ({
      ...current,
      [name]: type === 'checkbox' ? checked : value
    }));
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setError('');
    setIsSubmitting(true);

    try {
      await signIn(form);
      navigate(location.state?.from?.pathname || '/screenings', { replace: true });
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Unable to log in.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section className="row justify-content-center">
      <div className="col-md-7 col-lg-5">
        <div className="app-shell p-4">
          <h1 className="h3 mb-3">Login</h1>
          {error && (
            <div className="alert alert-danger" role="alert">
              {error}
            </div>
          )}
          <form onSubmit={handleSubmit}>
            <div className="mb-3">
              <label className="form-label" htmlFor="email">
                Email
              </label>
              <input
                autoComplete="username"
                className="form-control"
                id="email"
                name="email"
                onChange={updateField}
                required
                type="email"
                value={form.email}
              />
            </div>
            <div className="mb-3">
              <label className="form-label" htmlFor="password">
                Password
              </label>
              <input
                autoComplete="current-password"
                className="form-control"
                id="password"
                name="password"
                onChange={updateField}
                required
                type="password"
                value={form.password}
              />
            </div>
            <div className="form-check mb-3">
              <input
                className="form-check-input"
                id="rememberMe"
                name="rememberMe"
                onChange={updateField}
                type="checkbox"
                checked={form.rememberMe}
              />
              <label className="form-check-label" htmlFor="rememberMe">
                Remember me
              </label>
            </div>
            <button className="btn btn-primary w-100" disabled={isSubmitting} type="submit">
              {isSubmitting ? 'Logging in...' : 'Login'}
            </button>
          </form>
          <p className="mb-0 mt-3 text-center">
            <Link to="/register">Create an account</Link>
          </p>
        </div>
      </div>
    </section>
  );
}
