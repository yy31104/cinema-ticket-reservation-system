import { useEffect, useState } from 'react';
import { ApiError, getProfile, updateProfile } from '../api/client.js';

export default function ProfilePage() {
  const [form, setForm] = useState({ name: '', surname: '', phoneNumber: '', rowVersion: '' });
  const [email, setEmail] = useState('');
  const [error, setError] = useState('');
  const [status, setStatus] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);

  function applyProfile(profile) {
    setEmail(profile.email);
    setForm({
      name: profile.name || '',
      surname: profile.surname || '',
      phoneNumber: profile.phoneNumber || '',
      rowVersion: profile.rowVersion ?? ''
    });
  }

  useEffect(() => {
    let isMounted = true;

    async function loadProfile() {
      setIsLoading(true);
      setError('');
      try {
        const profile = await getProfile();
        if (isMounted) {
          applyProfile(profile);
        }
      } catch (requestError) {
        if (isMounted) {
          setError(requestError instanceof ApiError ? requestError.message : 'Unable to load profile.');
        }
      } finally {
        if (isMounted) {
          setIsLoading(false);
        }
      }
    }

    loadProfile();
    return () => {
      isMounted = false;
    };
  }, []);

  function updateField(event) {
    const { name, value } = event.target;
    setForm((current) => ({ ...current, [name]: value }));
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setIsSaving(true);
    setError('');
    setStatus('');

    try {
      const updated = await updateProfile(form);
      const latest = updated?.rowVersion === undefined ? await getProfile() : updated;
      applyProfile(latest);
      setStatus('Profile updated successfully.');
    } catch (requestError) {
      if (requestError instanceof ApiError && requestError.status === 409) {
        const current = requestError.details?.current;
        if (current) {
          setForm({
            name: current.name || '',
            surname: current.surname || '',
            phoneNumber: current.phoneNumber || '',
            rowVersion: current.rowVersion ?? ''
          });
        }
        setError(requestError.message);
      } else {
        setError(requestError instanceof ApiError ? requestError.message : 'Unable to update profile.');
      }
    } finally {
      setIsSaving(false);
    }
  }

  if (isLoading) {
    return <div className="text-muted">Loading profile...</div>;
  }

  return (
    <section className="app-shell p-4 placeholder-panel">
      <h1 className="h3">Profile</h1>
      <p className="text-muted mb-4">{email}</p>

      {error && (
        <div className="alert alert-danger" role="alert">
          {error}
        </div>
      )}
      {status && (
        <div className="alert alert-success" role="alert">
          {status}
        </div>
      )}

      <form onSubmit={handleSubmit}>
        <div className="mb-3">
          <label className="form-label" htmlFor="name">
            Name
          </label>
          <input className="form-control" id="name" name="name" onChange={updateField} value={form.name} />
        </div>
        <div className="mb-3">
          <label className="form-label" htmlFor="surname">
            Surname
          </label>
          <input className="form-control" id="surname" name="surname" onChange={updateField} value={form.surname} />
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
        <button className="btn btn-primary" disabled={isSaving} type="submit">
          {isSaving ? 'Saving...' : 'Save profile'}
        </button>
      </form>
    </section>
  );
}
