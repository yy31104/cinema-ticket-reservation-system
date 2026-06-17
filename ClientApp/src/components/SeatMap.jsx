import { useEffect, useRef, useState } from 'react';
import SeatButton from './SeatButton.jsx';
import SeatLegend from './SeatLegend.jsx';

function getSeatKey(seat) {
  return `${seat.rowNumber}-${seat.seatNumber}`;
}

function isSameSeat(firstSeat, secondSeat) {
  return firstSeat?.rowNumber === secondSeat?.rowNumber && firstSeat?.seatNumber === secondSeat?.seatNumber;
}

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

function findSeatPosition(seatRows, seatKey) {
  for (let rowIndex = 0; rowIndex < seatRows.length; rowIndex += 1) {
    const seatIndex = seatRows[rowIndex].seats.findIndex((seat) => getSeatKey(seat) === seatKey);

    if (seatIndex !== -1) {
      return { rowIndex, seatIndex };
    }
  }

  return null;
}

function getSeatKeyAt(seatRows, rowIndex, seatIndex) {
  const row = seatRows[rowIndex];
  if (!row) {
    return '';
  }

  const seat = row.seats[Math.min(Math.max(seatIndex, 0), row.seats.length - 1)];
  return seat ? getSeatKey(seat) : '';
}

export default function SeatMap({
  onCancel,
  onClearSelection,
  onConfirmReservation,
  onSelectSeat,
  pendingSeatKey,
  selectedSeat,
  seatMap
}) {
  const buttonRefs = useRef(new Map());
  const seatsPerRow = Number(seatMap.cinema?.seatsPerRow || 1);
  const summary = getSeatSummary(seatMap.seatRows);
  const hasCenterAisle = seatsPerRow >= 8;
  const aisleAfterIndex = Math.ceil(seatsPerRow / 2) - 1;
  const firstSeatKey = seatMap.seatRows[0]?.seats[0] ? getSeatKey(seatMap.seatRows[0].seats[0]) : '';
  const [focusedSeatKey, setFocusedSeatKey] = useState(firstSeatKey);
  const selectedSeatKey = selectedSeat ? getSeatKey(selectedSeat) : '';
  const isSelectionPending = selectedSeatKey !== '' && pendingSeatKey === selectedSeatKey;

  useEffect(() => {
    if (!firstSeatKey) {
      setFocusedSeatKey('');
      return;
    }

    if (!findSeatPosition(seatMap.seatRows, focusedSeatKey)) {
      setFocusedSeatKey(firstSeatKey);
    }
  }, [firstSeatKey, focusedSeatKey, seatMap.seatRows]);

  function focusSeat(seatKey) {
    if (!seatKey) {
      return;
    }

    setFocusedSeatKey(seatKey);
    window.requestAnimationFrame(() => {
      buttonRefs.current.get(seatKey)?.focus();
    });
  }

  function handleSeatKeyDown(event, seatKey) {
    const currentPosition = findSeatPosition(seatMap.seatRows, seatKey);
    if (!currentPosition) {
      return;
    }

    let nextSeatKey = '';
    const isNavigationKey = ['ArrowRight', 'ArrowLeft', 'ArrowDown', 'ArrowUp', 'Home', 'End'].includes(event.key);

    if (event.key === 'ArrowRight') {
      nextSeatKey = getSeatKeyAt(seatMap.seatRows, currentPosition.rowIndex, currentPosition.seatIndex + 1);
    } else if (event.key === 'ArrowLeft') {
      nextSeatKey = getSeatKeyAt(seatMap.seatRows, currentPosition.rowIndex, currentPosition.seatIndex - 1);
    } else if (event.key === 'ArrowDown') {
      nextSeatKey = getSeatKeyAt(seatMap.seatRows, currentPosition.rowIndex + 1, currentPosition.seatIndex);
    } else if (event.key === 'ArrowUp') {
      nextSeatKey = getSeatKeyAt(seatMap.seatRows, currentPosition.rowIndex - 1, currentPosition.seatIndex);
    } else if (event.key === 'Home') {
      nextSeatKey = getSeatKeyAt(seatMap.seatRows, currentPosition.rowIndex, 0);
    } else if (event.key === 'End') {
      nextSeatKey = getSeatKeyAt(
        seatMap.seatRows,
        currentPosition.rowIndex,
        seatMap.seatRows[currentPosition.rowIndex].seats.length - 1
      );
    }

    if (isNavigationKey) {
      event.preventDefault();
    }

    if (nextSeatKey && nextSeatKey !== seatKey) {
      focusSeat(nextSeatKey);
    }
  }

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
          <span>{summary.reserved} taken</span>
          {summary.mine > 0 && <span>{summary.mine} yours</span>}
        </div>
      </div>

      <SeatLegend />

      {selectedSeat && (
        <div className="selected-seat-panel" aria-live="polite">
          <div>
            <p className="selected-seat-kicker">Selected seat</p>
            <p className="selected-seat-value">
              Row {selectedSeat.rowNumber}, Seat {selectedSeat.seatNumber}
            </p>
            <p className="selected-seat-help">Confirm when you are ready to reserve this seat.</p>
          </div>
          <div className="selected-seat-actions">
            <button
              className="btn btn-primary btn-sm"
              disabled={isSelectionPending}
              onClick={onConfirmReservation}
              type="button"
            >
              {isSelectionPending ? 'Reserving...' : 'Confirm reservation'}
            </button>
            <button
              className="btn btn-outline-secondary btn-sm"
              disabled={isSelectionPending}
              onClick={onClearSelection}
              type="button"
            >
              Clear selection
            </button>
          </div>
        </div>
      )}

      <p className="visually-hidden" id="screen-cue">
        The screen is at the front of the auditorium before the first row of seats.
      </p>

      <div className="auditorium" role="region" aria-label="Cinema auditorium seat map" aria-describedby="screen-cue">
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
                    const key = getSeatKey(seat);
                    const isAisleSeat = hasCenterAisle && index === aisleAfterIndex;

                    return (
                      <div className={isAisleSeat ? 'seat-cell seat-cell-aisle-after' : 'seat-cell'} key={key}>
                        <SeatButton
                          buttonRef={(element) => {
                            if (element) {
                              buttonRefs.current.set(key, element);
                            } else {
                              buttonRefs.current.delete(key);
                            }
                          }}
                          isPending={pendingSeatKey === key}
                          isSelected={isSameSeat(seat, selectedSeat)}
                          onCancel={onCancel}
                          onFocus={() => setFocusedSeatKey(key)}
                          onKeyDown={(event) => handleSeatKeyDown(event, key)}
                          onSelect={onSelectSeat}
                          seat={seat}
                          tabIndex={focusedSeatKey === key || (!focusedSeatKey && key === firstSeatKey) ? 0 : -1}
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
