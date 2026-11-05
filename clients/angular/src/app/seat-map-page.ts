import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CinemaApi, errorMessage } from './cinema-api';
import { AuthUser, Seat, SeatMap } from './models';

interface Notice {
  kind: 'ok' | 'error';
  text: string;
}

@Component({
  selector: 'app-seat-map-page',
  imports: [FormsModule, RouterLink, DatePipe],
  template: `
    <p><a routerLink="/">← All screenings</a></p>

    @if (user() === undefined) {
      <p class="muted">Loading…</p>
    } @else if (user() === null) {
      <section class="panel">
        <h1>Sign in to choose seats</h1>
        <form (ngSubmit)="signIn()" class="sign-in">
          <label>Email <input name="email" type="email" autocomplete="username" [(ngModel)]="email" required /></label>
          <label>
            Password
            <input name="password" type="password" autocomplete="current-password" [(ngModel)]="password" required />
          </label>
          <button type="submit" [disabled]="busy()">Sign in</button>
        </form>
      </section>
    } @else if (seatMap(); as map) {
      <h1>{{ map.filmTitle }}</h1>
      <p class="meta">{{ map.startTime | date: 'EEE d MMM, HH:mm' }} · {{ map.cinema.name }}</p>

      <div class="screen" aria-hidden="true">Screen</div>
      <div class="seat-grid" role="grid" aria-label="Seat map">
        @for (row of map.seatRows; track row.rowNumber) {
          <div class="seat-row" role="row">
            <span class="row-label">{{ row.rowNumber }}</span>
            @for (seat of row.seats; track seat.seatNumber) {
              <button
                type="button"
                role="gridcell"
                class="seat"
                [class.mine]="seat.status === 'reservedByCurrentUser'"
                [class.taken]="seat.status === 'reservedByOtherUser'"
                [class.selected]="isSelected(seat)"
                [disabled]="seat.status !== 'free' || busy()"
                [attr.aria-pressed]="isSelected(seat)"
                [attr.aria-label]="label(seat)"
                (click)="toggle(seat)"
              >
                {{ seat.seatNumber }}
              </button>
            }
          </div>
        }
      </div>

      <div class="actions">
        <button type="button" (click)="reserve()" [disabled]="!selected() || busy()">
          @if (selected(); as s) {
            Reserve row {{ s.rowNumber }} seat {{ s.seatNumber }}
          } @else {
            Select a seat
          }
        </button>
        <span class="muted">Signed in as {{ user()!.email }}</span>
      </div>
    }

    @if (notice(); as n) {
      <p class="notice" [class.error]="n.kind === 'error'" role="status">{{ n.text }}</p>
    }
  `,
})
export class SeatMapPage {
  private readonly api = inject(CinemaApi);
  private readonly screeningId = Number(inject(ActivatedRoute).snapshot.paramMap.get('id'));

  /** undefined while loading, null when signed out. */
  protected readonly user = signal<AuthUser | null | undefined>(undefined);
  protected readonly seatMap = signal<SeatMap | null>(null);
  protected readonly selectedKey = signal<string | null>(null);
  protected readonly notice = signal<Notice | null>(null);
  protected readonly busy = signal(false);

  protected readonly selected = computed(() => {
    const key = this.selectedKey();
    const map = this.seatMap();
    if (!key || !map) {
      return null;
    }
    return map.seatRows.flatMap((r) => r.seats).find((s) => this.key(s) === key) ?? null;
  });

  protected email = '';
  protected password = '';

  constructor() {
    void this.load();
  }

  protected async signIn(): Promise<void> {
    await this.run(async () => {
      this.user.set(await this.api.login(this.email, this.password));
      this.password = '';
      await this.refreshSeats();
    });
  }

  protected toggle(seat: Seat): void {
    this.notice.set(null);
    this.selectedKey.update((current) => (current === this.key(seat) ? null : this.key(seat)));
  }

  protected async reserve(): Promise<void> {
    const seat = this.selected();
    if (!seat) {
      return;
    }
    await this.run(async () => {
      try {
        const reservation = await this.api.reserve(this.screeningId, seat.rowNumber, seat.seatNumber);
        this.notice.set({ kind: 'ok', text: `Reserved row ${reservation.rowNumber} seat ${reservation.seatNumber}.` });
      } finally {
        // Refresh either way: on a 409 the map shows who won the seat.
        this.selectedKey.set(null);
        await this.refreshSeats();
      }
    });
  }

  protected isSelected(seat: Seat): boolean {
    return this.selectedKey() === this.key(seat);
  }

  protected label(seat: Seat): string {
    const state =
      seat.status === 'reservedByCurrentUser' ? 'yours' : seat.status === 'reservedByOtherUser' ? 'taken' : 'free';
    return `Row ${seat.rowNumber}, seat ${seat.seatNumber}, ${state}`;
  }

  private async load(): Promise<void> {
    await this.run(async () => {
      const user = await this.api.me();
      this.user.set(user);
      if (user) {
        await this.refreshSeats();
      }
    });
  }

  private async refreshSeats(): Promise<void> {
    this.seatMap.set(await this.api.getSeats(this.screeningId));
  }

  private async run(action: () => Promise<void>): Promise<void> {
    this.busy.set(true);
    try {
      await action();
    } catch (error) {
      this.notice.set({ kind: 'error', text: errorMessage(error) });
      if (this.user() === undefined) {
        this.user.set(null);
      }
    } finally {
      this.busy.set(false);
    }
  }

  private key(seat: Seat): string {
    return `${seat.rowNumber}-${seat.seatNumber}`;
  }
}
