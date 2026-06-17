import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { ApiError } from '../api/client.js';
import { useAuth } from '../auth/AuthContext.jsx';

const initialForm = {
  email: '',
  password: '',
  confirmPassword: '',
  name: '',
  surname: '',
  phoneNumber: ''
};

function formatValidationErrors(errors) {
  if (Array.isArray(errors)) {
    return errors.join(' ');
  }

  if (errors && typeof errors === 'object') {
    return Object.values(errors).flat().join(' ');
  }

  return '';
}

export default function RegisterPage() {
  const navigate = useNavigate();
  const { signUp } = useAuth();
  const [form, setForm] = useState(initialForm);
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  function updateField(event) {
    const { name, value } = event.target;
    setForm((current) => ({ ...current, [name]: value }));
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setError('');

    if (form.password !== form.confirmPassword) {
      setError('Passwords do not match.');
      return;
    }

    setIsSubmitting(true);
    try {
      await signUp({
        ...form,
        phoneNumber: form.phoneNumber.trim() || null
      });
      navigate('/screenings');
    } catch (requestError) {
      const apiMessage = requestError instanceof ApiError ? requestError.message : 'Unable to register.';
      const details = formatValidationErrors(requestError?.details?.errors);
      setError(details || apiMessage);
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section className="row justify-content-center">
      <div className="col-md-8 col-lg-6">
        <div className="app-shell p-4">
          <h1 className="h3 mb-3">Register</h1>
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
            <div className="row">
              <div className="col-md-6 mb-3">
                <label className="form-label" htmlFor="password">
                  Password
                </label>
                <input
                  autoComplete="new-password"
                  className="form-control"
                  id="password"
                  minLength={6}
                  name="password"
                  onChange={updateField}
                  required
                  type="password"
                  value={form.password}
                />
              </div>
              <div className="col-md-6 mb-3">
                <label className="form-label" htmlFor="confirmPassword">
                  Confirm password
                </label>
                <input
                  autoComplete="new-password"
                  className="form-control"
                  id="confirmPassword"
                  minLength={6}
                  name="confirmPassword"
                  onChange={updateField}
                  required
                  type="password"
                  value={form.confirmPassword}
                />
              </div>
            </div>
            <div className="row">
              <div className="col-md-6 mb-3">
                <label className="form-label" htmlFor="name">
                  Name
                </label>
                <input className="form-control" id="name" name="name" onChange={updateField} value={form.name} />
              </div>
              <div className="col-md-6 mb-3">
                <label className="form-label" htmlFor="surname">
                  Surname
                </label>
                <input className="form-control" id="surname" name="surname" onChange={updateField} value={form.surname} />
              </div>
            </div>
            <div className="mb-3">
              <label className="form-label" htmlFor="phoneNumber">
                Phone number
              </label>
              <input
                className="form-control"
                id="phoneNumber"
                name="phoneNumber"
                onChange={updateField}
                type="tel"
                value={form.phoneNumber}
              />
            </div>
            <button className="btn btn-primary w-100" disabled={isSubmitting} type="submit">
              {isSubmitting ? 'Creating account...' : 'Register'}
            </button>
          </form>
          <p className="mb-0 mt-3 text-center">
            <Link to="/login">Use an existing account</Link>
          </p>
        </div>
      </div>
    </section>
  );
}
