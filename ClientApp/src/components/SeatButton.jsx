function getSeatCopy(seat, isPending) {
  const position = `Row ${seat.rowNumber}, seat ${seat.seatNumber}`;

  if (seat.status === 'free') {
    return {
      ariaLabel: `${position}, available. Activate to reserve this seat.`,
      className: 'seat-button seat-free',
      isDisabled: isPending,
      stateLabel: isPending ? 'Wait' : 'Open',
      title: `Free seat R${seat.rowNumber}, S${seat.seatNumber}`
    };
  }

  if (seat.canCancel) {
    return {
      ariaLabel: `${position}, ${seat.reservedByCurrentUser ? 'reserved by you' : 'reserved'}. Activate to cancel this reservation.`,
      className: 'seat-button seat-mine',
      isDisabled: isPending,
      stateLabel: isPending ? 'Wait' : seat.reservedByCurrentUser ? 'Yours' : 'Cancel',
      title: `Cancel reservation for R${seat.rowNumber}, S${seat.seatNumber}`
    };
  }

  return {
    ariaLabel: `${position}, reserved.`,
    className: 'seat-button seat-taken',
    isDisabled: true,
    stateLabel: 'Taken',
    title: `Reserved seat R${seat.rowNumber}, S${seat.seatNumber}`
  };
}

export default function SeatButton({ isPending, onCancel, onReserve, seat }) {
  const seatCopy = getSeatCopy(seat, isPending);
  const handleClick = seat.status === 'free' ? () => onReserve(seat) : seat.canCancel ? () => onCancel(seat) : undefined;

  return (
    <button
      aria-label={seatCopy.ariaLabel}
      className={`${seatCopy.className}${isPending ? ' seat-pending' : ''}`}
      disabled={seatCopy.isDisabled}
      onClick={handleClick}
      title={seatCopy.title}
      type="button"
    >
      <span className="seat-number" aria-hidden="true">
        {seat.seatNumber}
      </span>
      <span className="seat-state-label" aria-hidden="true">
        {seatCopy.stateLabel}
      </span>
    </button>
  );
}
