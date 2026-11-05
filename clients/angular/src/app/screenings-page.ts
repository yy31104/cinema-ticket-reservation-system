import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CinemaApi, errorMessage } from './cinema-api';
import { ScreeningListItem } from './models';

@Component({
  selector: 'app-screenings-page',
  imports: [RouterLink, DatePipe],
  template: `
    <h1>Screenings</h1>

    @if (error()) {
      <p class="notice error" role="alert">{{ error() }}</p>
    } @else if (screenings() === null) {
      <p class="muted">Loading screenings…</p>
    } @else if (screenings()!.length === 0) {
      <p class="muted">No screenings scheduled.</p>
    } @else {
      <ul class="screening-list">
        @for (screening of screenings(); track screening.id) {
          <li class="screening-card" data-testid="screening-card">
            <h2>
              <a [routerLink]="['/screenings', screening.id]">{{ screening.filmTitle }}</a>
            </h2>
            <p class="meta">
              {{ screening.startTime | date: 'EEE d MMM, HH:mm' }} · {{ screening.cinema.name }}
              @if (screening.durationMinutes) {
                · {{ screening.durationMinutes }} min
              }
            </p>
            <p class="meta">{{ seatsLeft(screening) }} of {{ capacity(screening) }} seats free</p>
          </li>
        }
      </ul>
    }
  `,
})
export class ScreeningsPage {
  private readonly api = inject(CinemaApi);

  protected readonly screenings = signal<ScreeningListItem[] | null>(null);
  protected readonly error = signal<string | null>(null);

  constructor() {
    this.api
      .getScreenings()
      .then((list) => this.screenings.set(list))
      .catch((error) => this.error.set(errorMessage(error)));
  }

  protected capacity(screening: ScreeningListItem): number {
    return screening.cinema.rows * screening.cinema.seatsPerRow;
  }

  protected seatsLeft(screening: ScreeningListItem): number {
    return this.capacity(screening) - screening.reservationCount;
  }
}
