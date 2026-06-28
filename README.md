# Cinema Ticket Reservation System

[![CI](https://github.com/yy31104/cinema-ticket-reservation-system/actions/workflows/ci.yml/badge.svg)](https://github.com/yy31104/cinema-ticket-reservation-system/actions/workflows/ci.yml)

A full-stack cinema seat-reservation app: browse screenings, pick exact seats on an
interactive, cinema-style seat map, and manage reservations — built on ASP.NET Core (.NET 8),
EF Core, ASP.NET Core Identity, and a React (Vite) single-page front end.

> **Project history.** This started as a university (EGUI) ASP.NET Core MVC/React assignment.
> The original stable submission is preserved at the **`v0-school-submission`** git tag. Since
> then it has been upgraded incrementally — in small, reviewed, PR-sized steps — into a
> production-style portfolio project, without losing the ability to diff against that baseline.

---

## What the app does

- **Browse screenings** (anonymous): a poster-led, responsive listing with client-side
  filtering by date and cinema, plus loading, empty, and error states.
- **Authenticate**: register / log in / log out via cookie-based ASP.NET Core Identity. The
  first registered user becomes admin when no admin exists.
- **Reserve seats**: an interactive auditorium seat map with a `SCREEN` indicator, aisles, and
  accessible seat states. Reservation uses an explicit **select-then-confirm** flow; seat
  status is conveyed by text + shape, not color alone.
- **Manage account**: edit profile (name, surname, phone) with optimistic-concurrency
  protection.
- **Admin**: create/delete screenings (delete cascades reservations), manage users
  (list/edit/delete, cannot delete self), and cancel any reservation.

The product deliberately stops at a confirmed **reservation** — there is no payment processing
(see [Limitations](#known-limitations)).

## From school project to production-style

The upgrade focused on reliability, UX quality, and engineering rigor. Highlights delivered on
top of the baseline:

| Area | Upgrade |
|---|---|
| **Design** | Dark, "cinema after dark" design system driven by CSS tokens; restyled app shell, navigation, and components |
| **Browsing** | Poster-style screening cards with availability badges, filters, skeletons, and empty states |
| **Seat map** | Auditorium layout with screen indicator + aisles; keyboard-navigable, accessible seat grid; select-then-confirm reservation flow |
| **Content** | Optional film metadata on screenings (poster URL, synopsis, duration, genre, age rating) with server-side validation and a migration |
| **Testing** | xUnit integration tests over the real HTTP + EF + SQLite stack (27 tests) |
| **CI** | GitHub Actions building and testing backend + frontend on every PR and push to `main` |

Planning and design decisions are documented under [`docs/`](docs/):
[PRODUCT_BRIEF](docs/PRODUCT_BRIEF.md) ·
[ARCHITECTURE](docs/ARCHITECTURE.md) ·
[DESIGN_SYSTEM](docs/DESIGN_SYSTEM.md) ·
[ROADMAP](docs/ROADMAP.md) ·
[REVIEW_CHECKLIST](docs/REVIEW_CHECKLIST.md) ·
[INTERVIEW_NOTES](docs/INTERVIEW_NOTES.md)

## Tech stack

- **Backend**: ASP.NET Core MVC / Web API (.NET 8), ASP.NET Core Identity (cookie auth)
- **Data**: Entity Framework Core + SQLite, migrations in `Data/Migrations`
- **Frontend**: React 19 + Vite, React Router, Bootstrap baseline + custom design tokens
- **Tests**: xUnit + `WebApplicationFactory` + SQLite (in-memory connection)
- **CI**: GitHub Actions

The legacy ASP.NET MVC/Razor implementation is still present and intentionally preserved; the
React SPA talks to the backend exclusively through `/api/...` JSON endpoints.

## Architecture at a glance

A single ASP.NET Core host serves three things: the JSON API (`/api/*`), the React SPA build
(from `wwwroot/app`), and the original MVC/Razor pages. Cookie-based Identity returns **401/403**
for unauthenticated/forbidden API requests instead of redirecting to a login page. See
[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the full picture, request flows, and the
target/production architecture.

### Main entities

- `ApplicationUser` — Identity user + `Name`, `Surname`, `PhoneNumber`, `RowVersion`
- `Cinema` — `Name`, `Rows`, `SeatsPerRow` (three seeded cinemas)
- `Screening` — film title, local start time, and **optional** poster URL, synopsis, duration,
  genre, and age-rating metadata
- `Reservation` — `(ScreeningId, RowNumber, SeatNumber)` with a **unique** constraint

`Screening.StartTime` is treated as the cinema's **local wall-clock time** and stored
unconverted; UTC storage and venue-timezone modeling are deferred (see ADR-0006 in
[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)).

## Reliability & concurrency

Two independent mechanisms keep the app correct under concurrent use, and both are covered by
automated tests:

1. **Same-seat double-booking** is prevented by a **database-level unique index** on
   `(ScreeningId, RowNumber, SeatNumber)`. The reserve endpoint relies on the database to
   arbitrate races: concurrent attempts to take the same seat are resolved atomically, and the
   losing request is translated from the resulting `DbUpdateException` into **HTTP 409 Conflict**
   rather than a 500. The conflict test exercises this through the real SQLite index (the EF
   in-memory provider would not enforce it).

2. **Stale profile / user edits** use `ApplicationUser.RowVersion` as an EF Core
   **optimistic-concurrency token**. Clients echo the last-known `RowVersion`; a stale
   profile/admin update or delete returns **HTTP 409 Conflict** with the current server-side
   values in the response so the client can reconcile and retry. New `RowVersion` values are
   generated centrally in `ApplicationDbContext` on save.

## Getting started

Prerequisites: **.NET 8 SDK** and **Node 20+**. The EF CLI is handy for migrations
(`dotnet tool install --global dotnet-ef`).

### Development mode (two servers)

Run the backend and the Vite dev server separately. From the project root:

```bash
dotnet restore
dotnet ef database update   # creates the local app.db from migrations
dotnet run
```

In another terminal:

```bash
cd ClientApp
npm install
npm run dev
```

Open the Vite URL (usually `http://localhost:5173/screenings`). The dev server proxies `/api`
requests to the backend (default target `http://localhost:5101`; override with
`VITE_API_TARGET`).

### Production one-server mode

ASP.NET Core can serve the API, the React production build (`wwwroot/app`), and the legacy
MVC/Razor pages from one process:

```bash
cd ClientApp
npm install
npm run build
cd ..
dotnet run
```

Then open an ASP.NET URL, e.g. `http://localhost:5101/screenings`. For a packaged build,
`dotnet publish -c Release` runs `npm ci` + `npm run build` and includes the generated
`wwwroot/app` output. The React build output is generated and git-ignored.

## Running checks

Backend build and tests (from the project root):

```bash
dotnet build CinemaTicketReservation.sln
dotnet test CinemaTicketReservation.sln
```

Frontend build:

```bash
cd ClientApp
npm ci
npm run build
```

## Testing

Backend behavior is covered by **27 xUnit integration tests** under
[`tests/MVC.Tests`](tests/MVC.Tests). They run against the real ASP.NET Core pipeline via
`WebApplicationFactory`, backed by a **kept-open in-memory SQLite connection** (not the EF
in-memory provider) so relational constraints behave like production. Each test gets an isolated
database; the local `app.db` is never touched. Coverage includes:

- **Reservations** — reserve a free seat, same-seat conflict → **409**, cancel-and-rebook.
- **Concurrency** — profile and admin user edit/delete with current vs. stale `RowVersion`
  (stale → **409** with a `current` payload).
- **Poster URL validation** — accepts `https`/`http`/app-relative paths; rejects `javascript:`,
  protocol-relative `//host`, and backslash `/\host` URLs.
- **Screening metadata** — create with/without metadata; list and details include it.
- **Authorization** — anonymous, authenticated, and admin access paths (401/403/200) and the
  first-user-becomes-admin rule.

## Continuous integration

[`.github/workflows/ci.yml`](.github/workflows/ci.yml) runs on every pull request and on pushes
to `main` with read-only permissions and no secrets. It:

- builds and tests the backend against `CinemaTicketReservation.sln` (.NET 8), and
- installs and builds the frontend (`npm ci` + `npm run build`, Node 20) in `ClientApp`.

NuGet and npm caches are used to speed up runs. The badge at the top of this README reflects the
latest `main` status.

## Accounts

- The **first registered user becomes admin** when no admin exists.
- `Program.cs` also promotes `admin@example.com` / `user@example.com` to the `Admin` role **if**
  they already exist in the database. No passwords are stored in source; use whatever password
  was created for those accounts in the local `app.db`.

## Project structure

```
Controllers/            MVC + API controllers (Controllers/Api/* = JSON endpoints)
Models/                 Domain models, view models, API DTOs (Models/Api/*)
Data/                   ApplicationDbContext + EF Core migrations
Services/               AdminRoleService (role bootstrap)
Views/, Areas/Identity/ Legacy MVC/Razor + Identity pages (preserved)
ClientApp/              React (Vite) front end
wwwroot/app/            Generated React build (git-ignored)
tests/MVC.Tests/        xUnit integration tests
.github/workflows/      CI
docs/                   Product, architecture, design, roadmap, review, interview notes
```

## Known limitations

Stated honestly — these are intentional boundaries or planned future work, not hidden bugs:

- **No payment processing.** The flow ends at a confirmed reservation by design; there is no
  checkout, pricing, or payment provider.
- **SQLite only.** Great for local/demo. PostgreSQL is documented as a *future* production
  option in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) but is **not yet wired up**.
- **`StartTime` has no timezone model.** It is stored as local wall-clock time; multi-timezone
  correctness is deferred.
- **Automated tests are backend/API only.** There are no frontend unit tests yet; the React app
  is covered by `npm run build` in CI and manual verification.
- **Some UX is still baseline.** Destructive actions use `window.confirm` (toast/modal polish is
  planned), and there is no dedicated "My Reservations" page yet.
- **No API error standard / OpenAPI yet.** Errors use a consistent `ApiErrorDto` shape but not
  RFC 7807 ProblemDetails, and there is no Swagger UI.
- **No production hardening yet.** No security headers, rate limiting, or deployment automation
  beyond HTTPS redirect/HSTS.
- **Posters are URLs only.** The app stores poster URLs and renders them with a graceful
  placeholder fallback; it does not host, upload, or download images, and no copyrighted images
  are committed.

See [docs/ROADMAP.md](docs/ROADMAP.md) for what is done and what remains.

## Baseline

The original school submission is tagged **`v0-school-submission`**. Its history is never
rewritten, so every upgrade can be compared against it (`git diff v0-school-submission..main`).
```
