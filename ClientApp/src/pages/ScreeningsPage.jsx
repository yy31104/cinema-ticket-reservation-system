import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import EmptyState from '../components/EmptyState.jsx';
import LoadingSkeleton from '../components/LoadingSkeleton.jsx';
import ScreeningCard from '../components/ScreeningCard.jsx';
import { ApiError, deleteScreening, getScreenings } from '../api/client.js';
import { useAuth } from '../auth/AuthContext.jsx';

function formatDateTime(value) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short'
  }).format(new Date(value));
}

function formatDateLabel(value) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium'
  }).format(new Date(value));
}

function toDateKey(value) {
  const date = new Date(value);
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

export default function ScreeningsPage() {
  const { isAdmin } = useAuth();
  const [screenings, setScreenings] = useState([]);
  const [error, setError] = useState('');
  const [status, setStatus] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [deletingId, setDeletingId] = useState(null);
  const [filters, setFilters] = useState({
    cinema: 'all',
    date: 'all',
    search: ''
  });

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

  function updateFilter(event) {
    const { name, value } = event.target;
    setFilters((current) => ({
      ...current,
      [name]: value
    }));
  }

  function resetFilters() {
    setFilters({
      cinema: 'all',
      date: 'all',
      search: ''
    });
  }

  const sortedScreenings = useMemo(
    () => [...screenings].sort((first, second) => new Date(first.startTime) - new Date(second.startTime)),
    [screenings]
  );

  const nextScreening = sortedScreenings[0];
  const cinemaOptions = useMemo(
    () =>
      Array.from(new Set(sortedScreenings.map((screening) => screening.cinema?.name).filter(Boolean))).sort((first, second) =>
        first.localeCompare(second)
      ),
    [sortedScreenings]
  );
  const dateOptions = useMemo(() => {
    const options = new Map();
    for (const screening of sortedScreenings) {
      const key = toDateKey(screening.startTime);
      if (!options.has(key)) {
        options.set(key, formatDateLabel(screening.startTime));
      }
    }
    return Array.from(options.entries());
  }, [sortedScreenings]);

  const filteredScreenings = useMemo(() => {
    const normalizedSearch = filters.search.trim().toLowerCase();

    return sortedScreenings.filter((screening) => {
      const matchesCinema = filters.cinema === 'all' || screening.cinema?.name === filters.cinema;
      const matchesDate = filters.date === 'all' || toDateKey(screening.startTime) === filters.date;
      const matchesSearch = !normalizedSearch || (screening.filmTitle || '').toLowerCase().includes(normalizedSearch);

      return matchesCinema && matchesDate && matchesSearch;
    });
  }, [filters, sortedScreenings]);

  const hasActiveFilters = filters.cinema !== 'all' || filters.date !== 'all' || filters.search.trim().length > 0;

  return (
    <section className="screenings-page">
      <div className="screenings-hero">
        <div className="screenings-hero-copy">
          <span className="eyebrow">Now showing</span>
          <h1>Choose your next seat in the dark.</h1>
          <p>
            Browse upcoming screenings, filter by cinema or date, and jump straight into the live seat map when you find
            the right showtime.
          </p>
          {nextScreening && (
            <p className="screenings-hero-next">
              Next up: <strong>{nextScreening.filmTitle}</strong> at {formatDateTime(nextScreening.startTime)}
            </p>
          )}
        </div>
        {isAdmin && (
          <Link className="btn btn-primary" to="/admin/screenings/create">
            Create screening
          </Link>
        )}
      </div>

      {error && (
        <div className="alert alert-danger screenings-alert" role="alert">
          <div>{error}</div>
          <button className="btn btn-outline-danger btn-sm" onClick={loadScreenings} type="button">
            Retry
          </button>
        </div>
      )}
      {status && (
        <div className="alert alert-success" role="status">
          {status}
        </div>
      )}

      {isLoading ? (
        <LoadingSkeleton />
      ) : screenings.length === 0 ? (
        <EmptyState
          action={
            isAdmin ? (
              <Link className="btn btn-primary" to="/admin/screenings/create">
                Create screening
              </Link>
            ) : null
          }
          title="No screenings yet"
        >
          Check back soon for new showtimes.
        </EmptyState>
      ) : (
        <>
          <div className="screenings-toolbar app-shell">
            <div>
              <label className="form-label" htmlFor="screeningSearch">
                Search film
              </label>
              <input
                className="form-control"
                id="screeningSearch"
                name="search"
                onChange={updateFilter}
                placeholder="Search by title"
                type="search"
                value={filters.search}
              />
            </div>
            <div>
              <label className="form-label" htmlFor="screeningDate">
                Date
              </label>
              <select className="form-select" id="screeningDate" name="date" onChange={updateFilter} value={filters.date}>
                <option value="all">All dates</option>
                {dateOptions.map(([value, label]) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="form-label" htmlFor="screeningCinema">
                Cinema
              </label>
              <select
                className="form-select"
                id="screeningCinema"
                name="cinema"
                onChange={updateFilter}
                value={filters.cinema}
              >
                <option value="all">All cinemas</option>
                {cinemaOptions.map((cinema) => (
                  <option key={cinema} value={cinema}>
                    {cinema}
                  </option>
                ))}
              </select>
            </div>
            <div className="screenings-toolbar-summary">
              <span>
                Showing {filteredScreenings.length} of {screenings.length}
              </span>
              {hasActiveFilters && (
                <button className="btn btn-outline-secondary btn-sm" onClick={resetFilters} type="button">
                  Clear filters
                </button>
              )}
            </div>
          </div>

          {filteredScreenings.length === 0 ? (
            <EmptyState
              action={
                <button className="btn btn-outline-secondary" onClick={resetFilters} type="button">
                  Clear filters
                </button>
              }
              title="No matching screenings"
            >
              Try a different film title, date, or cinema.
            </EmptyState>
          ) : (
            <div className="screenings-grid">
              {filteredScreenings.map((screening, index) => (
                <ScreeningCard
                  deletingId={deletingId}
                  index={index}
                  isAdmin={isAdmin}
                  key={screening.id}
                  onDelete={handleDelete}
                  screening={screening}
                />
              ))}
            </div>
          )}
        </>
      )}
    </section>
  );
}
