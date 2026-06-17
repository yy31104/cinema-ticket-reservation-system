export default function LoadingSkeleton({ count = 6 }) {
  return (
    <div className="screenings-grid" aria-label="Loading screenings">
      {Array.from({ length: count }, (_, index) => (
        <article className="screening-card screening-card-skeleton" key={index}>
          <div className="poster-frame skeleton-pulse" />
          <div className="screening-card-body">
            <div className="skeleton-line skeleton-line-lg" />
            <div className="skeleton-line" />
            <div className="skeleton-line skeleton-line-sm" />
          </div>
        </article>
      ))}
    </div>
  );
}
