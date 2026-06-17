import SeatButton from './SeatButton.jsx';
import SeatLegend from './SeatLegend.jsx';

function getSeatSummary(seatRows) {
  const seats = seatRows.flatMap((row) => row.seats);
  const available = seats.filter((seat) => seat.status === 'free').length;
  const mine = seats.filter((seat) => seat.reservedByCurrentUser).length;

  return {
    available,
    mine,
    reserved: Math.max(seats.length - available - mine, 0),
    total: seats.length
  };
}

export default function SeatMap({ onCancel, onReserve, pendingSeatKey, seatMap }) {
  const seatsPerRow = Number(seatMap.cinema?.seatsPerRow || 1);
  const summary = getSeatSummary(seatMap.seatRows);
  const hasCenterAisle = seatsPerRow >= 8;
  const aisleAfterIndex = Math.ceil(seatsPerRow / 2) - 1;

  return (
    <section className="seat-map-panel" aria-labelledby="seat-map-heading">
      <div className="seat-map-header">
        <div>
          <p className="eyebrow mb-1">Seat selection</p>
          <h2 className="h4 mb-0" id="seat-map-heading">
            Choose your place
          </h2>
        </div>
        <div className="seat-map-counts" aria-label="Seat availability summary">
          <span>{summary.available} open</span>
          <span>{summary.reserved} reserved</span>
          {summary.mine > 0 && <span>{summary.mine} yours</span>}
        </div>
      </div>

      <SeatLegend />

      <div className="auditorium" role="region" aria-label="Cinema auditorium seat map">
        <div className="screen-stage" aria-hidden="true">
          <div className="screen-indicator">
            <span>SCREEN</span>
          </div>
        </div>

        <div className="seat-map-viewport">
          <div className="seat-map" style={{ '--seat-count': seatsPerRow }}>
            {seatMap.seatRows.map((row) => (
              <div className="seat-row" key={row.rowNumber}>
                <div className="seat-row-label" aria-label={`Row ${row.rowNumber}`}>
                  R{row.rowNumber}
                </div>
                <div className="seat-row-grid">
                  {row.seats.map((seat, index) => {
                    const key = `${seat.rowNumber}-${seat.seatNumber}`;
                    const isAisleSeat = hasCenterAisle && index === aisleAfterIndex;

                    return (
                      <div className={isAisleSeat ? 'seat-cell seat-cell-aisle-after' : 'seat-cell'} key={key}>
                        <SeatButton
                          isPending={pendingSeatKey === key}
                          onCancel={onCancel}
                          onReserve={onReserve}
                          seat={seat}
                        />
                      </div>
                    );
                  })}
                </div>
              </div>
            ))}
          </div>
        </div>

        {summary.total === 0 && <p className="text-muted mb-0">No seats are configured for this screening.</p>}
      </div>
    </section>
  );
}
