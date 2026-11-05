// Mirrors the DTOs in Models/Api/*.cs. The Angular client reuses the same
// JSON API as the React client in ClientApp/; nothing here is Angular-specific.

export interface Cinema {
  id: number;
  name: string;
  rows: number;
  seatsPerRow: number;
}

export interface ScreeningListItem {
  id: number;
  filmTitle: string;
  startTime: string;
  posterUrl: string | null;
  synopsis: string | null;
  durationMinutes: number | null;
  genre: string | null;
  ageRating: string | null;
  cinema: Cinema;
  reservationCount: number;
}

export type SeatStatus = 'free' | 'reservedByCurrentUser' | 'reservedByOtherUser';

export interface Seat {
  rowNumber: number;
  seatNumber: number;
  status: SeatStatus;
  reservationId: number | null;
  reservedByCurrentUser: boolean;
  canCancel: boolean;
}

export interface SeatMap {
  screeningId: number;
  filmTitle: string;
  startTime: string;
  cinema: Cinema;
  seatRows: { rowNumber: number; seats: Seat[] }[];
}

export interface Reservation {
  id: number;
  screeningId: number;
  rowNumber: number;
  seatNumber: number;
  createdAt: string;
  reservedByCurrentUser: boolean;
}

export interface AuthUser {
  id: string;
  email: string;
  name: string | null;
  isAdmin: boolean;
}
