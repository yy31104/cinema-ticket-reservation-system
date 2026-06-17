# Architecture — Cinema Ticket Reservation System

> Status: Planning (architect-authored). Describes the **current** architecture as built in
> `v0-school-submission`, then the **target** architecture and a backend improvement plan.
> This document does not change code; implementation happens via the PRs in
> [ROADMAP.md](./ROADMAP.md).

## 1. Current architecture (as-is)

### 1.1 High-level shape

A single ASP.NET Core (.NET 8) host serves three things from one process:

```
                       ┌──────────────────────────────────────────────┐
   Browser             │              ASP.NET Core (.NET 8)            │
 ┌─────────┐  HTTPS    │                                              │
 │ React   │──────────►│  Middleware: HTTPS redirect, StaticFiles,    │
 │  SPA    │  /api/... │   React-route interceptor, Routing, AuthN/Z  │
 │ (Vite)  │◄──────────│                                              │
 └─────────┘  JSON     │  ┌──────────────┐   ┌────────────────────┐  │
      ▲                │  │ API Controllers│  │ MVC + Razor (legacy)│  │
      │ static         │  │  /api/*        │  │  Views/, Identity   │  │
      │ assets         │  └──────┬─────────┘  └─────────┬──────────┘  │
 ┌────┴─────┐          │         │ EF Core              │             │
 │wwwroot/  │◄─────────│         ▼                      ▼             │
 │  app/    │  served  │   ApplicationDbContext  ──────────────────► SQLite (app.db)
 └──────────┘          │   (Identity + domain)                       │
                       └──────────────────────────────────────────────┘
```

- **React SPA** (`ClientApp/`, React 19 + Vite 7 + React Router 7 + Bootstrap 5.3) is the
  primary UI. It talks to the backend exclusively through `/api/*` JSON endpoints.
- **Legacy MVC/Razor** (`Controllers/*.cs`, `Views/`, `Areas/Identity/`) remains from the
  school baseline and must keep working until a task explicitly removes it.
- **One-server mode:** `Program.cs` intercepts known SPA GET routes and returns the built
  `wwwroot/app/index.html`; everything else falls through to MVC/Razor or the API.
- **Persistence:** EF Core over **SQLite** (`app.db`), migrations in `Data/Migrations`.

### 1.2 Request routing in `Program.cs`

- A custom middleware (`IsReactAppRoute`) matches an allow-list of SPA GET routes
  (`/login`, `/register`, `/screenings`, `/screenings/{int}`, `/profile`, `/admin/users`,
  `/admin/users/{id}`, `/admin/screenings/create`) and serves the SPA shell.
- API requests (`/api/*`) are detected so that, instead of redirecting unauthenticated users
  to the Identity login page, the cookie middleware returns **401** (unauthorized) and **403**
  (forbidden) — correct behavior for an SPA/JSON client.
- `AdminRoleService.EnsureAdminRoleAsync` runs at startup to guarantee the `Admin` role exists
  and to apply the admin fallback (see §1.5).

> Architectural note: the SPA route allow-list in `Program.cs` is **duplicated** with the
> route table in `ClientApp/src/App.jsx`. Any new SPA route must be added in both places.
> The target architecture proposes a catch-all fallback to remove this coupling (§2.4).

### 1.3 Domain model

```
Cinema (1) ───< (N) Screening (1) ───< (N) Reservation (N) >─── (1) ApplicationUser
  Id                 Id                       Id                       Id (Identity)
  Name               FilmTitle                ScreeningId              Name
  Rows               StartTime                UserId                   Surname
  SeatsPerRow        CinemaId                 RowNumber                PhoneNumber
                                              SeatNumber               RowVersion (concurrency)
                                              CreatedAt
```

Key EF Core configuration (`Data/ApplicationDbContext.cs`):

- `Reservation` has a **unique index** on `(ScreeningId, RowNumber, SeatNumber)` — the
  backbone of double-booking prevention.
- `Screening → Cinema`: `OnDelete(Restrict)` (cannot delete a cinema with screenings).
- `Reservation → Screening`: `OnDelete(Cascade)` (deleting a screening removes its
  reservations).
- `Reservation → User`: `OnDelete(Restrict)` (a user with reservations cannot be hard-deleted
  until reservations are gone — surfaced as a 409 in `UsersApiController.DeleteUser`).
- `ApplicationUser.RowVersion` is a **concurrency token** with `ValueGeneratedNever`; a new
  `Guid`-derived value is assigned centrally in `SaveChanges`/`SaveChangesAsync` on add/modify.
- Seed data: three fixed cinemas (City Center 8×12, Riverside 10×14, Old Town 6×10).

### 1.4 API surface (current)

| Method | Route | Auth | Purpose | Notable responses |
|---|---|---|---|---|
| POST | `/api/auth/register` | Anon | Create account, sign in, maybe become admin | 201 / 400 |
| POST | `/api/auth/login` | Anon | Cookie sign-in | 200 / 401 / 403 |
| POST | `/api/auth/logout` | Auth | Sign out | 200 |
| GET | `/api/auth/me` | Auth | Current user + roles | 200 / 401 |
| GET | `/api/profile` | Auth | Own profile (+ RowVersion) | 200 / 401 |
| PUT | `/api/profile` | Auth | Update own profile | 200 / 400 / 401 / **409** |
| GET | `/api/users` | Admin | List users | 200 / 401 / 403 |
| GET | `/api/users/{id}` | Admin | User details | 200 / 404 |
| PUT | `/api/users/{id}` | Admin | Update user | 200 / 400 / 404 / **409** |
| DELETE | `/api/users/{id}` | Admin | Delete user (not self) | 200 / 400 / 403 / 404 / **409** |
| GET | `/api/screenings` | Anon | List screenings (+ reservation count) | 200 |
| GET | `/api/screenings/cinemas` | Anon | List cinemas | 200 |
| GET | `/api/screenings/{id}` | Anon | Screening details | 200 / 404 |
| GET | `/api/screenings/{id}/seats` | Auth | Seat map for current user | 200 / 401 / 404 |
| POST | `/api/screenings` | Admin | Create screening | 201 / 400 |
| DELETE | `/api/screenings/{id}` | Admin | Delete screening (cascades) | 200 / 404 |
| POST | `/api/screenings/{id}/reservations` | Auth | Reserve a seat | 201 / 400 / 404 / **409** |
| DELETE | `/api/screenings/{id}/reservations/{rid}` | Auth | Cancel (own or admin) | 200 / 403 / 404 |

All error bodies use a consistent `ApiErrorDto { message, errors[] }` (or an inline anonymous
object that includes `current` for concurrency conflicts).

### 1.5 Authentication & authorization

- **ASP.NET Core Identity** with cookie authentication; `RequireConfirmedAccount = false`.
- Single role: **`Admin`**. API authorization via `[Authorize]` and `[Authorize(Roles="Admin")]`.
- **Admin bootstrap** (`Services/AdminRoleService.cs`):
  - Ensures the `Admin` role exists.
  - Promotes known seed emails (`admin@example.com`, `user@example.com`) if present.
  - If no admin exists, promotes the **first registered user** (by Id order at startup, and on
    first registration via `EnsureFirstUserIsAdminAsync`).
- Frontend mirrors auth state in `AuthContext` (`/api/auth/me` on load) and gates routes with
  `RequireAuth` / `RequireAdmin`. **Server-side checks are authoritative**; the UI gating is a
  convenience only.

### 1.6 Concurrency model

Two independent mechanisms:

1. **Seat double-booking** — prevented at the database layer by the unique index. The reserve
   endpoint attempts the insert and catches `DbUpdateException`, inspecting the SQLite error
   (code 19 + the index/constraint name) to return **409 Conflict** rather than 500. This is
   robust under true concurrency because the database, not the app, arbitrates.
2. **User edit/delete** — optimistic concurrency via `ApplicationUser.RowVersion`. The client
   echoes the last-known `RowVersion`; the server sets it as the `OriginalValue` before save.
   A `DbUpdateConcurrencyException` becomes **409**, with the current server-side values
   returned so the client can reconcile and retry.

### 1.7 Frontend structure

```
ClientApp/src/
  main.jsx              app bootstrap (AuthProvider + Router)
  App.jsx              route table (mirrors Program.cs SPA allow-list)
  api/client.js        fetch wrapper + typed ApiError + all endpoint calls
  auth/AuthContext.jsx session state (user, isAdmin, isAuthenticated, sign in/up/out)
  components/          NavigationBar, RequireAuth/RequireAdmin
  pages/              Screenings, ScreeningDetails, Login, Register, Profile,
                       AdminUsers, AdminUserEdit, AdminScreeningCreate
  styles.css          small custom CSS on top of Bootstrap (seat map etc.)
```

Patterns in use: `credentials: 'include'` for cookie auth, a single `apiRequest` wrapper that
throws a typed `ApiError`, and local component state for loading/error/status. There is **no
global data cache** and **no toast system** (inline Bootstrap alerts + `window.confirm`).

### 1.8 Current gaps (drivers for the upgrade)

| Area | Gap |
|---|---|
| Visual | Bootstrap-default look; data tables; no design tokens; not "premium" |
| Content | No film metadata (poster, synopsis, runtime, genre, age rating) |
| Browse | No search/filter/grouping; no skeletons; thin empty states |
| Booking | No select-then-confirm; no "my reservations"; no booking reference |
| Seat map | Functional but plain; color-only status; limited a11y; no screen/aisles |
| API | Ad-hoc error shapes; no OpenAPI/Swagger; no ProblemDetails standard |
| Quality | **No automated tests**; **no CI**; no structured logging |
| Coupling | SPA route list duplicated in `Program.cs` and `App.jsx` |
| Security | Only HSTS + HTTPS redirect; no security headers; no rate limiting |
| Ops | SQLite only; no documented Postgres path; no deploy guide/Dockerfile |

## 2. Target architecture (to-be)

The target keeps the **same stack and single-host deployment** but raises engineering and
product quality. Changes are additive and incremental.

### 2.1 Layering (lightweight, not over-engineered)

Introduce a thin **service layer** for reservation/booking logic so controllers stay slim and
business rules become unit-testable, without adopting heavy DDD/CQRS abstractions:

```
API Controller  →  Application Service  →  EF Core (DbContext)
   (HTTP only)       (rules, mapping)        (persistence)
```

- Controllers: parse/validate input, call a service, map results to HTTP status codes.
- Services: e.g. `ReservationService` (reserve/cancel with conflict translation),
  `ScreeningService`, `BookingQueryService`. Keep the DB-arbitrated seat constraint as the
  source of truth; the service translates exceptions to domain results.
- Mapping: a single, consistent DTO mapping approach (manual mappers or a small profile),
  avoiding leaking entities to the client.

> Apply this **only where it earns its keep** (reservations first). Do not refactor every
> controller in one pass — that violates the PR-sized rule.

### 2.2 Standard API contract

- Adopt **RFC 7807 ProblemDetails** for error responses across `/api/*`, while preserving the
  existing fields clients already read (`message`, and `current` for concurrency). Provide a
  consistent envelope and document status-code semantics:
  - 400 validation, 401 unauthenticated, 403 forbidden, 404 not found,
    **409 conflict** (seat taken / stale RowVersion), 201 created.
- Add **OpenAPI/Swagger** (dev environment only) so the API is self-documenting and testable.
- Centralize model validation (data annotations + a validation filter) and return uniform 400s.

### 2.3 Data layer

- **SQLite stays** the default for local/demo. Keep `app.db` disposable and Git-ignored.
- Make the EF provider **configuration-selectable** so **PostgreSQL** can be enabled for
  production via connection string/provider switch — documented, not the default, no secrets
  committed.
- All schema changes remain **migration-backed** and documented in the PR. New content
  (film metadata) is a deliberate, seeded, migration-driven change.
- Introduce a **test database strategy**: SQLite (file or in-memory) per test run, or the
  EF in-memory provider for service tests, with a seeded fixture.

### 2.4 Routing & SPA hosting

- Replace the hand-maintained SPA route allow-list with a **catch-all SPA fallback** for
  non-API, non-static GET requests (e.g. `MapFallbackToFile`), so new React routes don't
  require editing `Program.cs`. Keep `/api/*` and existing MVC/Razor routes intact.
- Keep legacy MVC/Razor reachable until an explicit task retires it.

### 2.5 Frontend architecture

- **Design system layer**: CSS custom properties (design tokens) + a small set of reusable
  components (Button, Card, Field, Toast, Modal, Badge, Skeleton, SeatButton). Keep Bootstrap
  as the baseline grid/utility layer until the design-system PRs supersede specific pieces.
- **Notifications**: a lightweight toast/notification context to replace `window.confirm` and
  `alert`, plus a reusable confirmation **Modal**.
- **Data fetching**: keep the current `apiRequest` wrapper; optionally centralize
  loading/error handling with small hooks (`useAsync`) — no new heavy data-layer dependency
  unless a task justifies it.
- **Accessibility**: focus management, ARIA roles for the seat grid, non-color status cues
  (see [DESIGN_SYSTEM.md](./DESIGN_SYSTEM.md#accessibility)).

### 2.6 Target API additions (small, behavior-additive)

| Method | Route | Auth | Purpose |
|---|---|---|---|
| GET | `/api/profile/reservations` (or `/api/reservations/mine`) | Auth | List signed-in user's reservations for "My Reservations" |
| (extended) | `/api/screenings*` | — | Carry film metadata fields once schema lands |

Exact shape is finalized in the relevant ROADMAP PR.

## 3. Backend improvement plan

Sequenced to match [ROADMAP.md](./ROADMAP.md); each is PR-sized.

1. **Error & validation standardization** — ProblemDetails envelope, validation filter,
   documented status-code contract; preserve existing client-read fields.
2. **OpenAPI/Swagger (dev)** — Swashbuckle in Development only; annotate controllers.
3. **Service layer for reservations** — extract `ReservationService`; keep DB constraint as
   source of truth; unit-test conflict translation.
4. **Film metadata** — extend the schema (poster, synopsis, runtime, genre, age rating) with a
   migration + seed; update DTOs and admin create form.
5. **My-reservations endpoint** — add the read endpoint backing the new user page.
6. **Provider configurability** — make SQLite/PostgreSQL selectable via configuration; document
   the Postgres path; no secrets committed.
7. **Security hardening** — security headers, basic rate limiting on auth endpoints, review of
   cookie/SameSite settings, server-side validation audit.
8. **Observability** — structured logging (request/auth/reservation outcomes), consistent log
   scopes; no PII in logs.

Non-goals for the backend plan: switching ORMs, adding message queues/caches, microservices,
or real payment/checkout logic.

## 4. Testing strategy

Layered, with the highest value on the concurrency-critical paths.

- **Backend unit/integration (xUnit)** — top priority:
  - **Seat conflict → 409**: two reservations for the same `(screening,row,seat)`; second
    returns 409 (DB-arbitrated).
  - **RowVersion concurrency → 409**: stale profile/user update/delete returns 409 with
    `current` payload.
  - **Authorization matrix**: anonymous/authenticated/admin access to each endpoint returns
    the right 200/401/403.
  - **Admin bootstrap**: first user becomes admin when none exists; not otherwise.
  - **Cascade**: deleting a screening removes its reservations; deleting a user with
    reservations is blocked/handled.
  - Use `WebApplicationFactory` for endpoint tests against a fresh SQLite/in-memory DB.
- **Frontend (Vitest + React Testing Library)**:
  - Seat map renders states (free/mine/reserved) and disables non-actionable seats.
  - Reserve flow shows success; 409 shows conflict message and refreshes.
  - Auth gating: protected routes redirect when unauthenticated.
  - Form validation and error rendering on login/register/profile.
- **Manual verification checklist** per PR for UX states (loading/empty/success/error/conflict)
  and keyboard/screen-reader spot checks.

Document any skipped test with a concrete reason (per AGENTS.md).

## 5. CI strategy

A single **GitHub Actions** workflow, required before merge:

```
on: [pull_request, push to main]
jobs:
  backend:  dotnet restore → dotnet build -warnaserror → dotnet test
  frontend: npm ci → npm run build → npm test → (lint if configured)
```

- Cache NuGet and npm.
- Fail the build on test failures and (where reasonable) compiler warnings.
- Keep secrets out of CI; SQLite/in-memory only.
- Optionally add a lint/format check (e.g. `dotnet format --verify-no-changes`, ESLint) as a
  separate non-blocking-then-blocking step.

## 6. Deployment

- **Local/demo (default):** one-server mode — build React → `dotnet run`/`dotnet publish`;
  SQLite `app.db`. This must always work from a clean checkout per the README.
- **Production (documented option):** PostgreSQL via connection string; run EF migrations on
  deploy; serve the published one-server output behind a reverse proxy with HTTPS. An optional
  Dockerfile/compose may be added by an explicit task. No managed/paid services are required to
  run the project.

## 7. Configuration & secrets

- Connection strings and environment-specific settings via `appsettings.{Environment}.json`,
  environment variables, or user-secrets in development.
- **Never commit** `app.db*`, `.env*`, real secret-bearing `appsettings.*` overrides, or
  generated `wwwroot/app/`, `bin/`, `obj/`, `node_modules/` (see AGENTS.md list).
- The Postgres connection string for production is provided at deploy time, never in Git.

## 8. Architecture decision log (seed)

Record significant decisions as short ADR-style entries in future PRs. Initial entries:

- **ADR-0001** Keep single-host (API + SPA + legacy MVC) deployment. *Rationale:* matches
  baseline, simple to run, sufficient for the product scope.
- **ADR-0002** Keep DB-arbitrated seat uniqueness as the source of truth for double-booking.
  *Rationale:* correct under true concurrency; simpler and safer than app-level locking.
- **ADR-0003** SQLite default, PostgreSQL as a configurable production option. *Rationale:*
  zero-setup local demo without precluding production.
- **ADR-0004** No real payments; reservation is the terminal step, with a documented seam for
  future checkout. *Rationale:* explicit product constraint.
