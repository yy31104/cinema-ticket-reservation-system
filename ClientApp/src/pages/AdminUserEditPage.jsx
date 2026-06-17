import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { ApiError, deleteUser, getUser, updateUser } from '../api/client.js';

export default function AdminUserEditPage() {
  const navigate = useNavigate();
  const { id } = useParams();
  const [form, setForm] = useState({ name: '', surname: '', phoneNumber: '', rowVersion: '' });
  const [email, setEmail] = useState('');
  const [error, setError] = useState('');
  const [status, setStatus] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);

  function applyUser(user) {
    setEmail(user.email);
    setForm({
      name: user.name || '',
      surname: user.surname || '',
      phoneNumber: user.phoneNumber || '',
      rowVersion: user.rowVersion ?? ''
    });
  }

  useEffect(() => {
    let isMounted = true;

    async function loadUser() {
      setIsLoading(true);
      setError('');
      try {
        const user = await getUser(id);
        if (isMounted) {
          applyUser(user);
        }
      } catch (requestError) {
        if (isMounted) {
          setError(requestError instanceof ApiError ? requestError.message : 'Unable to load user.');
        }
      } finally {
        if (isMounted) {
          setIsLoading(false);
        }
      }
    }

    loadUser();
    return () => {
      isMounted = false;
    };
  }, [id]);

  function updateField(event) {
    const { name, value } = event.target;
    setForm((current) => ({ ...current, [name]: value }));
  }

  function loadCurrentUserFromConflict(requestError) {
    const current = requestError.details?.current;
    if (!current) {
      return false;
    }

    applyUser(current);
    return true;
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setIsSaving(true);
    setError('');
    setStatus('');

    try {
      const updated = await updateUser(id, form);
      const latest = updated?.rowVersion === undefined ? await getUser(id) : updated;
      applyUser(latest);
      setStatus('User updated successfully.');
    } catch (requestError) {
      if (requestError instanceof ApiError && requestError.status === 409) {
        loadCurrentUserFromConflict(requestError);
        setError(requestError.message);
      } else {
        setError(requestError instanceof ApiError ? requestError.message : 'Unable to update user.');
      }
    } finally {
      setIsSaving(false);
    }
  }

  async function handleDelete() {
    if (!window.confirm(`Delete user ${email}?`)) {
      return;
    }

    setIsDeleting(true);
    setError('');
    setStatus('');

    try {
      await deleteUser(id, form.rowVersion);
      navigate('/admin/users');
    } catch (requestError) {
      if (requestError instanceof ApiError && requestError.status === 409) {
        loadCurrentUserFromConflict(requestError);
        setError(requestError.message);
      } else {
        setError(requestError instanceof ApiError ? requestError.message : 'Unable to delete user.');
      }
    } finally {
      setIsDeleting(false);
    }
  }

  if (isLoading) {
    return <div className="text-muted">Loading user...</div>;
  }

  return (
    <section className="app-shell p-4 placeholder-panel">
      <div className="d-flex justify-content-between align-items-start gap-3 mb-3">
        <div>
          <h1 className="h3 mb-1">Edit User</h1>
          <div className="text-muted">{email}</div>
        </div>
        <Link className="btn btn-outline-secondary" to="/admin/users">
          Back
        </Link>
      </div>

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
        <div className="d-flex flex-wrap gap-2">
          <button className="btn btn-primary" disabled={isSaving || isDeleting} type="submit">
            {isSaving ? 'Saving...' : 'Save user'}
          </button>
          <button className="btn btn-outline-danger" disabled={isSaving || isDeleting} onClick={handleDelete} type="button">
            {isDeleting ? 'Deleting...' : 'Delete user'}
          </button>
        </div>
      </form>
    </section>
  );
}
