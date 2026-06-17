export default function EmptyState({ action, children, title }) {
  return (
    <div className="empty-state">
      <div className="empty-state-mark" aria-hidden="true">
        CINEMA
      </div>
      <h2>{title}</h2>
      <p>{children}</p>
      {action && <div className="empty-state-action">{action}</div>}
    </div>
  );
}
