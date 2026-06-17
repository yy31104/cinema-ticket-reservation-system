import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { ApiError, createScreening, getCinemas } from '../api/client.js';

function toDateTimeLocalValue(date) {
  const localDate = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
  return localDate.toISOString().slice(0, 16);
}

const initialStartTime = toDateTimeLocalValue(new Date(Date.now() + 60 * 60 * 1000));

function optionalText(value) {
  const trimmedValue = value.trim();
  return trimmedValue ? trimmedValue : null;
}

export default function AdminScreeningCreatePage() {
  const navigate = useNavigate();
  const [cinemas, setCinemas] = useState([]);
  const [form, setForm] = useState({
    filmTitle: '',
    startTime: initialStartTime,
    cinemaId: '',
    posterUrl: '',
    synopsis: '',
    durationMinutes: '',
    genre: '',
    ageRating: ''
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
        cinemaId: Number(form.cinemaId),
        posterUrl: optionalText(form.posterUrl),
        synopsis: optionalText(form.synopsis),
        durationMinutes: form.durationMinutes ? Number(form.durationMinutes) : null,
        genre: optionalText(form.genre),
        ageRating: optionalText(form.ageRating)
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
          <div className="mb-3">
            <label className="form-label" htmlFor="posterUrl">
              Poster URL
            </label>
            <input
              className="form-control"
              id="posterUrl"
              maxLength={2048}
              name="posterUrl"
              onChange={updateField}
              placeholder="https://example.com/poster.jpg or /app/posters/example.jpg"
              type="text"
              value={form.posterUrl}
            />
          </div>
          <div className="mb-3">
            <label className="form-label" htmlFor="synopsis">
              Synopsis
            </label>
            <textarea
              className="form-control"
              id="synopsis"
              maxLength={2000}
              name="synopsis"
              onChange={updateField}
              rows={4}
              value={form.synopsis}
            />
          </div>
          <div className="row">
            <div className="col-md-4 mb-3">
              <label className="form-label" htmlFor="durationMinutes">
                Duration minutes
              </label>
              <input
                className="form-control"
                id="durationMinutes"
                max={600}
                min={1}
                name="durationMinutes"
                onChange={updateField}
                type="number"
                value={form.durationMinutes}
              />
            </div>
            <div className="col-md-4 mb-3">
              <label className="form-label" htmlFor="genre">
                Genre
              </label>
              <input
                className="form-control"
                id="genre"
                maxLength={60}
                name="genre"
                onChange={updateField}
                value={form.genre}
              />
            </div>
            <div className="col-md-4 mb-3">
              <label className="form-label" htmlFor="ageRating">
                Age rating
              </label>
              <input
                className="form-control"
                id="ageRating"
                list="ageRatingOptions"
                maxLength={16}
                name="ageRating"
                onChange={updateField}
                value={form.ageRating}
              />
              <datalist id="ageRatingOptions">
                <option value="G" />
                <option value="PG" />
                <option value="PG-13" />
                <option value="R" />
              </datalist>
            </div>
          </div>
          <button className="btn btn-primary" disabled={isSubmitting || cinemas.length === 0} type="submit">
            {isSubmitting ? 'Creating...' : 'Create screening'}
          </button>
        </form>
      )}
    </section>
  );
}
