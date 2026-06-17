import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { ApiError, deleteScreening, getScreenings } from '../api/client.js';
import { useAuth } from '../auth/AuthContext.jsx';

function formatDate(value) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short'
  }).format(new Date(value));
}

export default function ScreeningsPage() {
  const { isAdmin } = useAuth();
  const [screenings, setScreenings] = useState([]);
  const [error, setError] = useState('');
  const [status, setStatus] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [deletingId, setDeletingId] = useState(null);

  const loadScreenings = useCallback(async () => {
    setIsLoading(true);
    setError('');
    try {
      const data = await getScreenings();
      setScreenings(data || []);
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Unable to load screenings.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    let isMounted = true;

    async function load() {
      setIsLoading(true);
      setError('');
      try {
        const data = await getScreenings();
        if (isMounted) {
          setScreenings(data || []);
        }
      } catch (requestError) {
        if (isMounted) {
          setError(requestError instanceof ApiError ? requestError.message : 'Unable to load screenings.');
        }
      } finally {
        if (isMounted) {
          setIsLoading(false);
        }
      }
    }

    load();
    return () => {
      isMounted = false;
    };
  }, []);

  async function handleDelete(screening) {
    if (!window.confirm(`Delete screening "${screening.filmTitle}" and all its reservations?`)) {
      return;
    }

    setDeletingId(screening.id);
    setError('');
    setStatus('');
    try {
      await deleteScreening(screening.id);
      setStatus('Screening deleted successfully.');
      await loadScreenings();
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Unable to delete screening.');
    } finally {
      setDeletingId(null);
    }
  }

  return (
    <section className="app-shell p-4">
      <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
        <h1 className="h3 mb-0">Screenings</h1>
        {isAdmin && (
          <Link className="btn btn-primary" to="/admin/screenings/create">
            Create screening
          </Link>
        )}
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

      {isLoading ? (
        <div className="text-muted">Loading screenings...</div>
      ) : screenings.length === 0 ? (
        <div className="text-muted">No screenings are currently available.</div>
      ) : (
        <div className="table-responsive">
          <table className="table table-striped align-middle mb-0">
            <thead>
              <tr>
                <th>Film title</th>
                <th>Start time</th>
                <th>Cinema</th>
                <th>Reserved seats</th>
                <th className="text-end">Actions</th>
              </tr>
            </thead>
            <tbody>
              {screenings.map((screening) => (
                <tr key={screening.id}>
                  <td>{screening.filmTitle}</td>
                  <td>{formatDate(screening.startTime)}</td>
                  <td>{screening.cinema?.name}</td>
                  <td>{screening.reservationCount}</td>
                  <td className="table-actions text-end">
                    <Link className="btn btn-sm btn-outline-primary" to={`/screenings/${screening.id}`}>
                      Details
                    </Link>
                    {isAdmin && (
                      <button
                        className="btn btn-sm btn-outline-danger ms-2"
                        disabled={deletingId === screening.id}
                        onClick={() => handleDelete(screening)}
                        type="button"
                      >
                        Delete
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
