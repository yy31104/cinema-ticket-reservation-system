import { useCallback, useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ApiError, cancelReservation, getScreening, getSeatMap, reserveSeat } from '../api/client.js';
import { useAuth } from '../auth/AuthContext.jsx';
import SeatMap from '../components/SeatMap.jsx';

function formatDate(value) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short'
  }).format(new Date(value));
}

function getFilmInitials(title) {
  return (title || 'Film')
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((word) => word[0])
    .join('')
    .toUpperCase();
}

function formatDuration(minutes) {
  if (!minutes) {
    return '';
  }

  const hours = Math.floor(minutes / 60);
  const remainingMinutes = minutes % 60;

  if (!hours) {
    return `${remainingMinutes}m`;
  }

  return remainingMinutes ? `${hours}h ${remainingMinutes}m` : `${hours}h`;
}

export default function ScreeningDetailsPage() {
  const { id } = useParams();
  const { isAuthenticated, isLoading: isAuthLoading } = useAuth();
  const [screening, setScreening] = useState(null);
  const [seatMap, setSeatMap] = useState(null);
  const [error, setError] = useState('');
  const [status, setStatus] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [isSeatMapLoading, setIsSeatMapLoading] = useState(false);
  const [pendingSeatKey, setPendingSeatKey] = useState('');
  const [selectedSeat, setSelectedSeat] = useState(null);
  const [isPosterUnavailable, setIsPosterUnavailable] = useState(false);

  const loadSeatMap = useCallback(async () => {
    if (!isAuthenticated) {
      setSeatMap(null);
      return;
    }

    setIsSeatMapLoading(true);
    try {
      const data = await getSeatMap(id);
      setSeatMap(data);
    } catch (requestError) {
      if (requestError instanceof ApiError && requestError.status === 401) {
        setError('Login is required to view and reserve seats.');
      } else {
        setError(requestError instanceof ApiError ? requestError.message : 'Unable to load seats.');
      }
    } finally {
      setIsSeatMapLoading(false);
    }
  }, [id, isAuthenticated]);

  useEffect(() => {
    let isMounted = true;

    async function loadDetails() {
      setIsLoading(true);
      setError('');
      try {
        const data = await getScreening(id);
        if (isMounted) {
          setScreening(data);
        }
      } catch (requestError) {
        if (isMounted) {
          setError(requestError instanceof ApiError ? requestError.message : 'Unable to load screening.');
        }
      } finally {
        if (isMounted) {
          setIsLoading(false);
        }
      }
    }

    loadDetails();
    return () => {
      isMounted = false;
    };
  }, [id]);

  useEffect(() => {
    if (!isAuthLoading) {
      loadSeatMap();
    }
  }, [isAuthLoading, loadSeatMap]);

  useEffect(() => {
    setIsPosterUnavailable(false);
  }, [screening?.posterUrl]);

  useEffect(() => {
    if (!selectedSeat || !seatMap) {
      return;
    }

    const currentSeat = seatMap.seatRows
      .flatMap((row) => row.seats)
      .find((seat) => seat.rowNumber === selectedSeat.rowNumber && seat.seatNumber === selectedSeat.seatNumber);

    if (!currentSeat || currentSeat.status !== 'free') {
      setSelectedSeat(null);
    }
  }, [seatMap, selectedSeat]);

  function handleSelectSeat(seat) {
    setError('');
    setStatus('');
    setSelectedSeat(seat);
  }

  function handleClearSelection() {
    setSelectedSeat(null);
  }

  function handleConfirmReservation() {
    if (!selectedSeat) {
      return;
    }

    handleReserve(selectedSeat);
  }

  async function handleReserve(seat) {
    const key = `${seat.rowNumber}-${seat.seatNumber}`;
    setPendingSeatKey(key);
    setError('');
    setStatus('');

    try {
      await reserveSeat(id, {
        rowNumber: seat.rowNumber,
        seatNumber: seat.seatNumber
      });
      setStatus(`Seat row ${seat.rowNumber}, seat ${seat.seatNumber} reserved.`);
      setSelectedSeat(null);
      await loadSeatMap();
    } catch (requestError) {
      if (requestError instanceof ApiError && requestError.status === 409) {
        setError('This seat was already reserved by another user.');
        setSelectedSeat(null);
        await loadSeatMap();
      } else {
        setError(requestError instanceof ApiError ? requestError.message : 'Unable to reserve seat.');
      }
    } finally {
      setPendingSeatKey('');
    }
  }

  async function handleCancel(seat) {
    if (!seat.reservationId) {
      return;
    }

    const key = `${seat.rowNumber}-${seat.seatNumber}`;
    setPendingSeatKey(key);
    setError('');
    setStatus('');

    try {
      await cancelReservation(id, seat.reservationId);
      setStatus(`Seat row ${seat.rowNumber}, seat ${seat.seatNumber} cancelled.`);
      await loadSeatMap();
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Unable to cancel reservation.');
    } finally {
      setPendingSeatKey('');
    }
  }

  if (isLoading) {
    return <div className="text-muted">Loading screening...</div>;
  }

  if (!screening) {
    return (
      <section className="app-shell p-4 placeholder-panel">
        <h1 className="h3">Screening Details</h1>
        <p className="text-muted">{error || 'Screening not found.'}</p>
        <Link className="btn btn-secondary" to="/screenings">
          Back to screenings
        </Link>
      </section>
    );
  }

  const metadataTags = [screening.genre, screening.ageRating, formatDuration(screening.durationMinutes)].filter(Boolean);
  const hasPoster = Boolean(screening.posterUrl) && !isPosterUnavailable;

  return (
    <section className="app-shell p-4">
      <div className="screening-detail-hero mb-4">
        <div className={`poster-frame poster-detail-frame poster-variant-0${hasPoster ? ' has-poster' : ''}`} aria-hidden="true">
          <div className="poster-frame-inner">
            <span className="poster-kicker">Now showing</span>
            <span className="poster-initials">{getFilmInitials(screening.filmTitle)}</span>
            <span className="poster-time">{formatDuration(screening.durationMinutes) || 'Cinema'}</span>
          </div>
          {hasPoster && (
            <img
              alt=""
              className="poster-image"
              onError={() => setIsPosterUnavailable(true)}
              src={screening.posterUrl}
            />
          )}
        </div>
        <div className="screening-detail-copy">
          <div className="d-flex flex-wrap justify-content-between align-items-start gap-3">
            <div>
              <h1 className="h3 mb-1">{screening.filmTitle}</h1>
              <div className="text-muted">{formatDate(screening.startTime)}</div>
            </div>
            <Link className="btn btn-outline-secondary" to="/screenings">
              Back
            </Link>
          </div>
          {metadataTags.length > 0 && (
            <div className="screening-detail-tags">
              {metadataTags.map((tag) => (
                <span className="metadata-pill" key={tag}>
                  {tag}
                </span>
              ))}
            </div>
          )}
          {screening.synopsis ? (
            <p className="screening-synopsis">{screening.synopsis}</p>
          ) : (
            <p className="screening-synopsis text-muted mb-0">Choose your seats for this screening.</p>
          )}
        </div>
      </div>

      <dl className="row mb-4">
        <dt className="col-sm-3">Cinema</dt>
        <dd className="col-sm-9">{screening.cinema?.name}</dd>
        <dt className="col-sm-3">Showtime</dt>
        <dd className="col-sm-9">{formatDate(screening.startTime)}</dd>
        <dt className="col-sm-3">Room size</dt>
        <dd className="col-sm-9">
          {screening.cinema?.rows} rows x {screening.cinema?.seatsPerRow} seats
        </dd>
        <dt className="col-sm-3">Reserved seats</dt>
        <dd className="col-sm-9">{screening.reservationCount}</dd>
      </dl>

      {status && (
        <div className="alert alert-success" role="status" aria-live="polite">
          {status}
        </div>
      )}
      {error && (
        <div className="alert alert-danger" role="alert" aria-live="assertive">
          {error}
        </div>
      )}

      {!isAuthenticated && !isAuthLoading && (
        <div className="alert alert-info" role="alert">
          Login to view the live seat map and make reservations.
        </div>
      )}

      {isSeatMapLoading ? (
        <div className="text-muted" role="status">
          Loading seats...
        </div>
      ) : seatMap ? (
        <SeatMap
          onClearSelection={handleClearSelection}
          onCancel={handleCancel}
          onConfirmReservation={handleConfirmReservation}
          onSelectSeat={handleSelectSeat}
          pendingSeatKey={pendingSeatKey}
          selectedSeat={selectedSeat}
          seatMap={seatMap}
        />
      ) : null}
    </section>
  );
}
