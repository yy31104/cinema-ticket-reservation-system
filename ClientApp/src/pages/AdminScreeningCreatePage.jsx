import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { ApiError, createScreening, getCinemas } from '../api/client.js';

function toDateTimeLocalValue(date) {
  const localDate = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
  return localDate.toISOString().slice(0, 16);
}

const initialStartTime = toDateTimeLocalValue(new Date(Date.now() + 60 * 60 * 1000));

export default function AdminScreeningCreatePage() {
  const navigate = useNavigate();
  const [cinemas, setCinemas] = useState([]);
  const [form, setForm] = useState({
    filmTitle: '',
    startTime: initialStartTime,
    cinemaId: ''
  });
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    let isMounted = true;

    async function loadCinemas() {
      setIsLoading(true);
      setError('');
      try {
        const data = await getCinemas();
        if (isMounted) {
          setCinemas(data || []);
          if (data?.length) {
            setForm((current) => ({ ...current, cinemaId: String(data[0].id) }));
          }
        }
      } catch (requestError) {
        if (isMounted) {
          setError(requestError instanceof ApiError ? requestError.message : 'Unable to load cinemas.');
        }
      } finally {
        if (isMounted) {
          setIsLoading(false);
        }
      }
    }

    loadCinemas();
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
    setIsSubmitting(true);
    setError('');

    try {
      const created = await createScreening({
        filmTitle: form.filmTitle,
        startTime: form.startTime,
        cinemaId: Number(form.cinemaId)
      });
      navigate(`/screenings/${created.id}`);
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Unable to create screening.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section className="app-shell p-4 placeholder-panel">
      <div className="d-flex justify-content-between align-items-start gap-3 mb-3">
        <h1 className="h3 mb-0">Create Screening</h1>
        <Link className="btn btn-outline-secondary" to="/screenings">
          Back
        </Link>
      </div>

      {error && (
        <div className="alert alert-danger" role="alert">
          {error}
        </div>
      )}

      {isLoading ? (
        <div className="text-muted">Loading cinemas...</div>
      ) : (
        <form onSubmit={handleSubmit}>
          <div className="mb-3">
            <label className="form-label" htmlFor="filmTitle">
              Film title
            </label>
            <input
              className="form-control"
              id="filmTitle"
              maxLength={200}
              name="filmTitle"
              onChange={updateField}
              required
              value={form.filmTitle}
            />
          </div>
          <div className="mb-3">
            <label className="form-label" htmlFor="startTime">
              Start time
            </label>
            <input
              className="form-control"
              id="startTime"
              name="startTime"
              onChange={updateField}
              required
              type="datetime-local"
              value={form.startTime}
            />
          </div>
          <div className="mb-3">
            <label className="form-label" htmlFor="cinemaId">
              Cinema
            </label>
            <select
              className="form-select"
              id="cinemaId"
              name="cinemaId"
              onChange={updateField}
              required
              value={form.cinemaId}
            >
              {cinemas.map((cinema) => (
                <option key={cinema.id} value={cinema.id}>
                  {cinema.name} ({cinema.rows} x {cinema.seatsPerRow})
                </option>
              ))}
            </select>
          </div>
          <button className="btn btn-primary" disabled={isSubmitting || cinemas.length === 0} type="submit">
            {isSubmitting ? 'Creating...' : 'Create screening'}
          </button>
        </form>
      )}
    </section>
  );
}
