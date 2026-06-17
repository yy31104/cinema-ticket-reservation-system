import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import StatusBadge from './StatusBadge.jsx';

function getFilmInitials(title) {
  return (title || 'Film')
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((word) => word[0])
    .join('')
    .toUpperCase();
}

function formatDateParts(value) {
  const date = new Date(value);
  return {
    day: new Intl.DateTimeFormat(undefined, { day: '2-digit' }).format(date),
    month: new Intl.DateTimeFormat(undefined, { month: 'short' }).format(date),
    time: new Intl.DateTimeFormat(undefined, { hour: '2-digit', minute: '2-digit' }).format(date),
    weekday: new Intl.DateTimeFormat(undefined, { weekday: 'short' }).format(date)
  };
}

function getAvailability(screening) {
  const capacity = Number(screening.cinema?.rows || 0) * Number(screening.cinema?.seatsPerRow || 0);
  const reserved = Number(screening.reservationCount || 0);

  if (!capacity) {
    return { label: 'Booking open', variant: 'info' };
  }

  const remaining = Math.max(capacity - reserved, 0);
  if (remaining === 0) {
    return { label: 'Sold out', variant: 'danger' };
  }

  if (remaining <= Math.max(5, Math.ceil(capacity * 0.15))) {
    return { label: `${remaining} seats left`, variant: 'warning' };
  }

  return { label: `${remaining} seats available`, variant: 'success' };
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

export default function ScreeningCard({ deletingId, index, isAdmin, onDelete, screening }) {
  const [isPosterUnavailable, setIsPosterUnavailable] = useState(false);
  const dateParts = formatDateParts(screening.startTime);
  const availability = getAvailability(screening);
  const posterVariant = index % 6;
  const isDeleting = deletingId === screening.id;
  const hasPoster = Boolean(screening.posterUrl) && !isPosterUnavailable;
  const metadataTags = [screening.genre, screening.ageRating, formatDuration(screening.durationMinutes)].filter(Boolean);

  useEffect(() => {
    setIsPosterUnavailable(false);
  }, [screening.posterUrl]);

  return (
    <article className="screening-card">
      <div className={`poster-frame poster-variant-${posterVariant}${hasPoster ? ' has-poster' : ''}`} aria-hidden="true">
        <div className="poster-frame-inner">
          <span className="poster-kicker">{dateParts.month}</span>
          <span className="poster-initials">{getFilmInitials(screening.filmTitle)}</span>
          <span className="poster-time">{dateParts.time}</span>
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
      <div className="screening-card-body">
        <div className="screening-card-meta">
          <StatusBadge variant={availability.variant}>{availability.label}</StatusBadge>
          <span>
            {dateParts.weekday} {dateParts.day}
          </span>
        </div>
        <h2 className="screening-card-title">{screening.filmTitle}</h2>
        {metadataTags.length > 0 && (
          <div className="screening-card-tags">
            {metadataTags.map((tag) => (
              <span className="metadata-pill" key={tag}>
                {tag}
              </span>
            ))}
          </div>
        )}
        <dl className="screening-card-details">
          <div>
            <dt>Cinema</dt>
            <dd>{screening.cinema?.name || 'Cinema TBA'}</dd>
          </div>
          <div>
            <dt>Showtime</dt>
            <dd>{dateParts.time}</dd>
          </div>
        </dl>
        <div className="screening-card-actions">
          <Link className="btn btn-primary btn-sm" to={`/screenings/${screening.id}`}>
            View seats
          </Link>
          {isAdmin && (
            <button
              className="btn btn-outline-danger btn-sm"
              disabled={isDeleting}
              onClick={() => onDelete(screening)}
              type="button"
            >
              {isDeleting ? 'Deleting...' : 'Delete'}
            </button>
          )}
        </div>
      </div>
    </article>
  );
}
