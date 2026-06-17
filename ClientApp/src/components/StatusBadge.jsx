export default function StatusBadge({ children, variant = 'neutral' }) {
  return <span className={`status-badge status-badge-${variant}`}>{children}</span>;
}
