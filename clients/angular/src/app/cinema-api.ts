import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AuthUser, Reservation, ScreeningListItem, SeatMap } from './models';

/**
 * Thin wrapper over the ASP.NET Core API. Requests are same-origin
 * (/api/...) and the dev server proxies them, so the Identity cookie
 * flows without CORS.
 */
@Injectable({ providedIn: 'root' })
export class CinemaApi {
  private readonly http = inject(HttpClient);

  getScreenings(): Promise<ScreeningListItem[]> {
    return firstValueFrom(this.http.get<ScreeningListItem[]>('/api/screenings'));
  }

  getSeats(screeningId: number): Promise<SeatMap> {
    return firstValueFrom(this.http.get<SeatMap>(`/api/screenings/${screeningId}/seats`));
  }

  reserve(screeningId: number, rowNumber: number, seatNumber: number): Promise<Reservation> {
    return firstValueFrom(
      this.http.post<Reservation>(`/api/screenings/${screeningId}/reservations`, { rowNumber, seatNumber }),
    );
  }

  /** Resolves to null when nobody is signed in. */
  async me(): Promise<AuthUser | null> {
    try {
      return await firstValueFrom(this.http.get<AuthUser>('/api/auth/me'));
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        return null;
      }
      throw error;
    }
  }

  login(email: string, password: string): Promise<AuthUser> {
    return firstValueFrom(this.http.post<AuthUser>('/api/auth/login', { email, password, rememberMe: false }));
  }
}

/** The API answers errors with { message }; fall back to the HTTP status. */
export function errorMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    const message = (error.error as { message?: unknown } | null)?.message;
    if (typeof message === 'string' && message.length > 0) {
      return message;
    }
    return `Request failed (${error.status}).`;
  }
  return 'Something went wrong.';
}
