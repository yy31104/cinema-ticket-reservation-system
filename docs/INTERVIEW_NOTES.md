# Interview Notes — Cinema Ticket Reservation System

Talking points for discussing this project in interviews. Everything here is grounded in the
repository as of `main` (latest: `Add RowVersion concurrency tests`). The project began as a
university (EGUI) ASP.NET Core MVC/React assignment — preserved at the `v0-school-submission`
tag — and was upgraded incrementally into a production-style portfolio app.

## 1. Five strongest technical talking points

1. **Database-arbitrated concurrency for seat booking.** Double-booking is prevented by a
   unique index on `(ScreeningId, RowNumber, SeatNumber)`, not by application-level locks. The
   API lets the database decide the winner of a race and translates the resulting
   `DbUpdateException` into a clean **HTTP 409**. This is correct under true concurrency and
   simpler than optimistic app-side checks.

2. **Two distinct, deliberate concurrency models.** Seat booking uses the DB unique constraint;
   user/profile edits use an EF Core `RowVersion` optimistic-concurrency token returning **409
   with the current server values**. I can explain *why* each path uses a different mechanism.

3. **Tests that exercise the real stack.** xUnit + `WebApplicationFactory` run against the
   actual HTTP pipeline, Identity auth, and EF Core — backed by a **kept-open in-memory SQLite
   connection** rather than the EF in-memory provider, specifically so relational constraints
   (the seat unique index) behave like production. The conflict test would fail under the EF
   in-memory provider; that's the point.

4. **Incremental, reviewed delivery against a preserved baseline.** Ten+ small PRs, each
   scoped to one concern, with an architecture/design doc set
   ([`docs/`](.)) and a review checklist driving every merge. The school baseline is a git tag,
   so any change is diffable against the original.

5. **Security-minded input validation with a regression test.** Poster URLs are validated
   server-side to allow only `http(s)` or app-relative paths, rejecting `javascript:`,
   protocol-relative `//host`, and the `/\host` backslash trick (which browsers normalize to
   protocol-relative). The fix shipped *with* a parameterized test asserting each case.

## 2. Concurrency story (the centerpiece)

**Same-seat conflict (database unique index → 409).**
- Schema: `Reservation` has a unique index on `(ScreeningId, RowNumber, SeatNumber)`
  (`Data/ApplicationDbContext.cs`).
- Flow: the reserve endpoint inserts a `Reservation` and `SaveChangesAsync()`; if a concurrent
  request already took the seat, the database rejects the insert. The controller catches
  `DbUpdateException`, inspects the SQLite error (constraint code + index name) to confirm it's
  the seat conflict, and returns **409 Conflict** with a clear message — otherwise it would be a
  500.
- Why DB-level: under concurrency, the database is the only component that can atomically
  arbitrate "who got the seat." App-side "check then insert" has a race window; the unique index
  has none.
- Tested: `ReservationTests.ReserveSameSeatBySecondUser_ReturnsConflict` — two *different*
  authenticated users reserve the same seat; first → 201, second → 409.

**Stale edits (RowVersion optimistic concurrency → 409).**
- `ApplicationUser.RowVersion` is an EF Core concurrency token; a fresh value is assigned
  centrally in `ApplicationDbContext.SaveChanges(Async)` on add/modify.
- Flow: clients read a record (with its `RowVersion`), then send it back on update/delete. The
  server sets it as the original value; if the row changed in the meantime, EF throws
  `DbUpdateConcurrencyException`, which the API turns into **409** and returns the **current**
  server-side values so the UI can reconcile.
- Tested: `ProfileConcurrencyTests` and `AdminUserConcurrencyTests` — current `RowVersion`
  succeeds (200); a stale `RowVersion` on profile update, admin user update, and admin user
  delete returns **409**, with the response body containing both a `message` and the `current`
  record (and the user still existing after a failed stale delete).

## 3. Testing & CI story

- **27 xUnit integration tests** under `tests/MVC.Tests`, grouped by concern: authorization,
  reservations, poster-URL validation, screening metadata, and the two concurrency suites.
- **Harness** (`CinemaApiFactory`): a `WebApplicationFactory<Program>` that swaps the EF
  provider to a single, kept-open `SqliteConnection("DataSource=:memory:")`, creates the schema
  *before* the host's startup admin-seed runs, and is disposed per test for full isolation. The
  local `app.db` is never opened.
- **Auth in tests** uses the real register → cookie pipeline (`WebApplicationFactoryClientOptions
  .HandleCookies`), and leans on "first registered user becomes admin" to get deterministic
  admin vs. non-admin clients.
- **CI** (`.github/workflows/ci.yml`): on every PR and push to `main`, with read-only
  permissions and no secrets — builds + tests the backend against the solution (.NET 8) and
  builds the frontend (`npm ci` + `npm run build`, Node 20), with NuGet/npm caching.
- One production touch was made to enable tests: `public partial class Program {}` so
  `WebApplicationFactory<Program>` can bind to the top-level-statements entry point.

## 4. Architecture overview

- **Single host, three surfaces.** One ASP.NET Core (.NET 8) process serves the JSON API
  (`/api/*`), the React (Vite) SPA build from `wwwroot/app`, and the original MVC/Razor pages.
  The React SPA is the primary UI and talks to the backend only via JSON.
- **Auth.** Cookie-based ASP.NET Core Identity. A small middleware tweak makes `/api/*` return
  **401/403** instead of redirecting to a login page — correct for an SPA/JSON client. Single
  `Admin` role; authorization enforced server-side with `[Authorize]`/roles, the UI gating is
  convenience only.
- **Data.** EF Core + SQLite; migrations in `Data/Migrations`. `Screening` carries optional film
  metadata directly (decision: no separate `Film` entity yet — see ADR-0005). `Cinema → Screening`
  is restrict-delete; `Screening → Reservation` is cascade-delete.
- **Frontend.** React Router SPA, a fetch wrapper with a typed `ApiError`, an `AuthContext`, and
  a CSS-token-based dark "cinema" design system layered over Bootstrap. The seat map is a
  keyboard-navigable grid with a select-then-confirm flow.
- Full details, request flows, and the target/production architecture live in
  [ARCHITECTURE.md](ARCHITECTURE.md).

## 5. Trade-offs & things intentionally not done

- **No payments.** Scope ends at a confirmed reservation; a "hold → checkout → confirm" seam is
  documented for the future but not built.
- **Metadata on `Screening`, not a `Film` entity.** Smaller, additive migration now; a `Film`
  aggregate is deferred until cross-screening reuse is needed (ADR-0005). Trade-off: metadata
  duplicates per screening at demo scale.
- **SQLite only.** Zero-setup local/demo; PostgreSQL is documented but not wired up.
- **`StartTime` stored as local wall-clock**, unconverted (ADR-0006). Avoids a cross-cutting
  timezone refactor; multi-timezone correctness is future work.
- **No seeded demo screenings.** `HasData` requires static dates that would go stale and couples
  to seeded cinema FKs; a relative-date startup seeder is left as a follow-up.
- **Backend-only tests.** Highest-risk logic (concurrency, authz, validation) first; frontend
  unit tests and a full ProblemDetails/OpenAPI contract are deferred.
- **Legacy MVC/Razor preserved.** Kept working rather than deleted, so the baseline diff stays
  meaningful; retiring it is a deliberate future decision.
- **UX still partly baseline.** `window.confirm` for destructive actions; toast/modal polish and
  a "My Reservations" page are planned, not done.

## 6. Ten interview Q&A bullets

- **Q: How do you prevent two people booking the same seat?** A unique DB index on
  `(ScreeningId, RowNumber, SeatNumber)`; the DB arbitrates the race and the loser gets a 409
  translated from the `DbUpdateException`. No app-level locking.

- **Q: Why not check seat availability in code before inserting?** "Check-then-insert" has a
  race window between the read and the write; the unique constraint closes it atomically.

- **Q: What's the difference between your two 409 paths?** Seats use a DB unique constraint
  (pessimistic at the storage layer); profile/user edits use a `RowVersion` optimistic token.
  Different problems, different tools.

- **Q: Why in-memory *SQLite* and not the EF in-memory provider for tests?** The EF in-memory
  provider doesn't enforce relational constraints, so the seat-conflict test would silently
  pass without proving anything. Real SQLite enforces the unique index.

- **Q: How do tests authenticate?** Through the real register/login cookie pipeline via
  `WebApplicationFactory`, with cookie handling on — no fake auth handler — so authorization is
  genuinely exercised.

- **Q: How does an SPA work with cookie-based Identity here?** Identity normally redirects to a
  login page; a middleware tweak makes `/api/*` return 401/403 instead, which an SPA can handle.

- **Q: Why add metadata to `Screening` instead of a `Film` table?** Smallest additive migration
  and no new relationships; a `Film` entity only pays off once the same film has many showtimes
  (documented as ADR-0005).

- **Q: How did you keep an admin from loading malicious poster URLs?** Server-side validation
  allows only `http(s)`/app-relative URLs and rejects `javascript:`, `//host`, and `/\host`;
  the front end also renders posters with an `onError` placeholder fallback, and text is escaped
  by React (no `dangerouslySetInnerHTML`).

- **Q: How do you guarantee you didn't regress the school baseline?** It's a git tag
  (`v0-school-submission`) that's never rewritten, every change is a small reviewed PR, and CI
  builds + tests on every PR.

- **Q: What would you do next if this were going to production?** Wire up a Postgres provider,
  add ProblemDetails + OpenAPI, security headers and rate limiting, frontend tests, and a
  proper timezone model for showtimes — all already scoped in the roadmap.
