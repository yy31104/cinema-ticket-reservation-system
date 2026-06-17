function getSeatCopy(seat, isPending, isSelected) {
  const position = `Row ${seat.rowNumber}, seat ${seat.seatNumber}`;

  if (seat.status === 'free' && isSelected) {
    return {
      ariaLabel: `${position}, selected. Confirm reservation below to reserve this seat.`,
      className: 'seat-button seat-selected',
      isDisabled: isPending,
      isUnavailable: false,
      stateLabel: isPending ? 'Wait' : 'Selected',
      title: `Selected seat R${seat.rowNumber}, S${seat.seatNumber}`
    };
  }

  if (seat.status === 'free') {
    return {
      ariaLabel: `${position}, available. Activate to select this seat.`,
      className: 'seat-button seat-free',
      isDisabled: isPending,
      isUnavailable: false,
      stateLabel: isPending ? 'Wait' : 'Open',
      title: `Free seat R${seat.rowNumber}, S${seat.seatNumber}`
    };
  }

  if (seat.canCancel) {
    return {
      ariaLabel: `${position}, ${seat.reservedByCurrentUser ? 'reserved by you' : 'reserved'}. Activate to cancel this reservation.`,
      className: 'seat-button seat-mine',
      isDisabled: isPending,
      isUnavailable: false,
      stateLabel: isPending ? 'Wait' : seat.reservedByCurrentUser ? 'Yours' : 'Cancel',
      title: `Cancel reservation for R${seat.rowNumber}, S${seat.seatNumber}`
    };
  }

  return {
    ariaLabel: `${position}, reserved by another user.`,
    className: 'seat-button seat-taken',
    isDisabled: false,
    isUnavailable: true,
    stateLabel: 'Taken',
    title: `Reserved seat R${seat.rowNumber}, S${seat.seatNumber}`
  };
}

export default function SeatButton({
  buttonRef,
  isPending,
  isSelected,
  onCancel,
  onFocus,
  onKeyDown,
  onSelect,
  seat,
  tabIndex
}) {
  const seatCopy = getSeatCopy(seat, isPending, isSelected);
  const handleClick = seat.status === 'free' ? () => onSelect(seat) : seat.canCancel ? () => onCancel(seat) : undefined;

  function handleKeyDown(event) {
    onKeyDown(event);

    if ((event.key === 'Enter' || event.key === ' ') && handleClick && !seatCopy.isDisabled && !seatCopy.isUnavailable) {
      event.preventDefault();
      handleClick();
    }
  }

  return (
    <button
      aria-label={seatCopy.ariaLabel}
      aria-pressed={seat.status === 'free' ? isSelected : undefined}
      aria-disabled={seatCopy.isUnavailable ? 'true' : undefined}
      className={`${seatCopy.className}${isPending ? ' seat-pending' : ''}`}
      disabled={seatCopy.isDisabled}
      onClick={handleClick}
      onFocus={onFocus}
      onKeyDown={handleKeyDown}
      ref={buttonRef}
      tabIndex={tabIndex}
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
