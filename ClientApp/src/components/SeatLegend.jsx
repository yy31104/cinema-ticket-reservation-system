const legendItems = [
  { className: 'seat-free', label: 'Available' },
  { className: 'seat-mine', label: 'Your reservation' },
  { className: 'seat-taken', label: 'Reserved' }
];

export default function SeatLegend() {
  return (
    <div className="seat-legend-list" aria-label="Seat status legend">
      {legendItems.map((item) => (
        <span className="seat-legend-item" key={item.label}>
          <span className={`seat-legend-swatch ${item.className}`} aria-hidden="true" />
          <span>{item.label}</span>
        </span>
      ))}
    </div>
  );
}
