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
      await loadSeatMap();
    } catch (requestError) {
      if (requestError instanceof ApiError && requestError.status === 409) {
        setError('This seat was already reserved by another user.');
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

  return (
    <section className="app-shell p-4">
      <div className="d-flex flex-wrap justify-content-between align-items-start gap-3 mb-4">
        <div>
          <h1 className="h3 mb-1">{screening.filmTitle}</h1>
          <div className="text-muted">{formatDate(screening.startTime)}</div>
        </div>
        <Link className="btn btn-outline-secondary" to="/screenings">
          Back
        </Link>
      </div>

      <dl className="row mb-4">
        <dt className="col-sm-3">Cinema</dt>
        <dd className="col-sm-9">{screening.cinema?.name}</dd>
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
          onCancel={handleCancel}
          onReserve={handleReserve}
          pendingSeatKey={pendingSeatKey}
          seatMap={seatMap}
        />
      ) : null}
    </section>
  );
}
