# Roadmap — Cinema Ticket Reservation System

> Status: Planning (architect-authored). A sequenced set of **PR-sized**, Codex-ready tasks
> that upgrade the project from `v0-school-submission` into a production-style portfolio
> product, per [PRODUCT_BRIEF.md](./PRODUCT_BRIEF.md), [ARCHITECTURE.md](./ARCHITECTURE.md),
> and [DESIGN_SYSTEM.md](./DESIGN_SYSTEM.md).

## How to use this roadmap (for Codex)

- Implement **one PR at a time**, in order, unless a dependency note says otherwise.
- Obey [AGENTS.md](../AGENTS.md): focused diffs, no unrelated refactors, no new dependencies
  unless the task explicitly authorizes them (and the PR explains why), no schema changes
  unless the task says so, keep the app runnable locally.
- Each PR must finish with the **report** required by [REVIEW_CHECKLIST.md](./REVIEW_CHECKLIST.md):
  files changed, commands run, risks, and the next PR.
- Request Claude's review before merge for product/UX/auth/data/security-significant changes.
- Branch naming: `feat/…`, `fix/…`, `docs/…`, `chore/…`, `test/…` (e.g. `feat/design-foundation`).

## Phases

| Phase | PRs | Theme |
|---|---|---|
| **A — Look like a product** | 01, 02 | Design foundation + cinematic browsing |
| **B — Be a real product** | 03, 04, 05 | Film content, premium seat map, bookings |
| **C — Engineer like a product** | 06, 07, 08 | API hardening, tests, CI |
| **D — Ship like a product** | 09, 10 | UX polish + production readiness |

Phase C (tests/CI) may begin in parallel after PR-01 if capacity allows; PR-07/08 do not
depend on the UI PRs.

## Delivery status (as of `main`)

This section tracks what has actually shipped vs. what remains. It reflects the git history on
`main` and is updated as PRs merge; it does not change the task specs below. Note the **merge
order differed from the numbering** — the premium seat map (PR-04) shipped before film metadata
(PR-03), and a concurrency-test follow-up (PR-07b) was added after the test foundation.

| Item | Status | Shipped as (commit subject) |
|---|---|---|
| PR-01 — Design system foundation & app shell | ✅ Done | Add design system foundation and app shell |
| PR-02 — Cinematic screenings listing | ✅ Done | Add cinematic screenings listing |
| PR-04 — Premium seat map (visual) | ✅ Done | Add cinema-style seat map layout |
| PR-04 — Premium seat map (select-then-confirm + keyboard) | ✅ Done | Add select-then-confirm seat reservation flow |
| PR-03 — Film metadata (schema + migration) | ✅ Done | Add screening film metadata fields |
| PR-07 — Automated testing foundation | ✅ Done | Add backend API testing foundation |
| PR-08 — Continuous integration | ✅ Done | Add GitHub Actions CI |
| PR-07b — RowVersion concurrency tests | ✅ Done | Add RowVersion concurrency tests |
| PR-05 — Booking confirmation & "My Reservations" | ⬜ Not started | — |
| PR-06 — API hardening (ProblemDetails + OpenAPI) | ⬜ Not started | — |
| PR-09 — UX polish (toasts, modals, auth/admin) | ⬜ Not started | — |
| PR-10 — Production readiness (provider config, security, deploy) | ⬜ Not started | — |

**Done:** the design system, cinematic browsing, the full premium seat map (visual + behavior),
optional film metadata with a migration, a 27-test backend integration suite (incl. seat-conflict
and RowVersion 409 coverage), and green CI on every PR/push to `main`.

**Future optional work** (not built — see also [INTERVIEW_NOTES.md](./INTERVIEW_NOTES.md) and the
README "Known limitations"):
- **PR-05** — a "My Reservations" page + booking confirmation/reference and a read endpoint.
- **PR-06** — RFC 7807 ProblemDetails error contract and OpenAPI/Swagger (dev).
- **PR-09** — toast notifications + modal confirmations replacing `window.confirm`; auth/admin polish.
- **PR-10** — configurable PostgreSQL provider, security headers, rate limiting, SPA catch-all
  fallback, and a deployment guide.
- **Smaller follow-ups** — frontend unit tests (Vitest/RTL); a relative-date demo-screening
  seeder; a `Film` entity if cross-screening reuse is needed; a timezone model for `StartTime`.

---

## PR-01 — Design system foundation & app shell

**Goal:** Replace the Bootstrap-default look with the cinematic dark theme. Establish tokens
and restyle the global shell. **Visual only — no behavior, route, schema, or dependency change.**

**Scope**
- Add `ClientApp/src/styles/tokens.css` with the tokens from
  [DESIGN_SYSTEM.md §2](./DESIGN_SYSTEM.md#2-design-tokens); import once at app entry.
- Apply the dark theme to `body`/app background; map key Bootstrap variables to tokens where
  practical.
- Restyle `NavigationBar` (sticky, dark, brand left, account/actions right, responsive).
- Restyle global primitives used everywhere: buttons, alerts, the `app-shell` panel,
  links, and the existing seat-map CSS to token colors (no markup/logic change).
- Add a simple footer if appropriate.

**Out of scope:** page layout rewrites, new components, new routes, behavior changes.

**Acceptance criteria**
- App renders in the dark cinematic theme across all existing pages with no visual regressions
  in functionality.
- No changes to `App.jsx` routes, API calls, or `Program.cs`.
- No new npm/NuGet dependencies.
- Contrast on primary text/surfaces meets the bar in
  [DESIGN_SYSTEM.md §9](./DESIGN_SYSTEM.md#9-accessibility); focus rings visible.
- Existing flows (login, reserve, admin) still work unchanged.

**Verification:** `cd ClientApp && npm run build`; `dotnet build`; manual click-through of every
page (desktop + mobile widths); screenshots attached.

**Risk:** Low. Pure styling. Watch for Bootstrap variable overrides leaking into unintended
components.

---

## PR-02 — Screenings as a cinematic listing

**Goal:** Turn the screenings table into a premium, poster-ready **card grid** with filtering
and proper states. Uses existing API only (no schema yet — poster falls back gracefully).

**Scope**
- Replace the table in `ScreeningsPage` with a responsive card grid
  ([DESIGN_SYSTEM.md §5.1](./DESIGN_SYSTEM.md#51-screenings-landing)).
- Add `Card`, `Badge`, `Skeleton`, and `EmptyState` components.
- Client-side **filter bar**: by date and cinema (data already available via
  `/api/screenings` + `/api/screenings/cinemas`); optional title search.
- Availability badge derived from `reservationCount` vs cinema capacity
  (`rows × seatsPerRow`): "Sold out" / "Few seats left".
- Poster area uses a graceful fallback (gradient + initials) until film metadata exists.
- Preserve admin "Create screening" entry and per-card delete (now via confirm modal if PR-09
  isn't done yet, otherwise keep `window.confirm` and note it for PR-09).

**Out of scope:** schema/film metadata (PR-03), seat map redesign (PR-04).

**Acceptance criteria**
- Responsive grid per [DESIGN_SYSTEM.md §3.1](./DESIGN_SYSTEM.md#31-responsive-breakpoints).
- Loading shows skeletons; no screenings shows `EmptyState`; errors show a retry affordance.
- Filtering by date/cinema works without a page reload and without new endpoints.
- Admin create/delete still function; deletion still confirms before acting.
- No backend or schema changes; no new dependencies.

**Verification:** `npm run build`; manual: empty, loading, error, populated, filtered,
admin-as-admin and as-guest; screenshots (desktop + mobile).

**Risk:** Low–medium. Availability math must match server semantics (count vs capacity).

**Depends on:** PR-01.

---

## PR-03 — Film metadata (schema + migration)

**Goal:** Make screenings feel like real films: poster, synopsis, runtime, genre, age rating.
**This task explicitly authorizes a schema change + migration + seed update.**

**Scope**
- Extend the model with film metadata. Choose the **simplest coherent option** and state it in
  the PR: either add fields to `Screening` (`PosterUrl`, `Synopsis`, `DurationMinutes`,
  `Genre`, `AgeRating`) **or** introduce a `Film` entity referenced by `Screening`.
  *Recommended:* add fields to `Screening` first (smaller diff); a `Film` entity can come later
  if reuse across showtimes is needed.
- Add an EF Core **migration** under `Data/Migrations`; update seed data with realistic sample
  films (use clearly-licensed or placeholder poster URLs / local placeholder assets — no
  copyrighted binaries committed).
- Update `ScreeningDtos`, the screenings list/details mapping, and the admin create form
  (`AdminScreeningCreatePage`) to capture and display the new fields.
- Render metadata in the card grid (PR-02) and the details page.

**Out of scope:** seat map redesign (PR-04); pricing.

**Acceptance criteria**
- Migration applies cleanly on a fresh DB (`dotnet ef database update`) and is reversible.
- New fields are server-side validated (lengths/ranges); details and cards show metadata;
  missing posters use the fallback.
- Admin can create a screening with metadata; existing reservations unaffected.
- `dotnet build` + `dotnet test` green; migration documented in the PR.
- No `app.db` or generated artifacts committed.

**Verification:** fresh DB migrate + seed; create a screening with metadata; view list/details;
`dotnet test`.

**Risk:** Medium (schema). Keep the migration minimal and additive; do not alter reservation
or identity tables.

**Depends on:** PR-02 (for rendering surfaces). Touches backend + frontend.

---

## PR-04 — Premium seat map

**Goal:** Elevate the seat map to the signature, accessible, select-then-confirm experience in
[DESIGN_SYSTEM.md §5.3](./DESIGN_SYSTEM.md#53-seat-map-signature-component). Reservation
behavior and the 409 contract are **preserved**.

**Scope**
- `SeatButton` + seat legend components using the state tokens
  ([DESIGN_SYSTEM.md §2.3](./DESIGN_SYSTEM.md#23-seat-map-state-tokens)).
- Add a **screen indicator**, row labels, and an aisle gap; availability summary.
- Implement **select → confirm** flow (select a free seat, see a summary, confirm). Keep the
  existing reserve/cancel API calls and the **409 conflict** handling (toast + auto-refresh).
- Full **keyboard navigation** of the grid and ARIA labels per
  [DESIGN_SYSTEM.md §9](./DESIGN_SYSTEM.md#9-accessibility); reserved-by-others seats are
  disabled with a non-color cue.

**Out of scope:** changing reservation API semantics; multi-seat-per-confirm may be a stretch
goal — if included, keep each seat a separate API call and handle partial 409s clearly.

**Acceptance criteria**
- Seat states match the design; status is conveyed without relying on color.
- Reserve still returns 201 on success and surfaces 409 with a refresh; cancel still works for
  own seats and admin.
- Seat map is fully keyboard-operable; focus visible; screen-reader labels correct.
- No backend change required (uses existing `/seats`, reserve, cancel endpoints).

**Verification:** two-session conflict test (reserve same seat → 409 surfaced); keyboard-only
walkthrough; screen-reader spot check; screenshots/GIF.

**Risk:** Medium. Concurrency UX and accessibility are the hard parts; do not regress the 409
flow.

**Depends on:** PR-01 (tokens). Independent of PR-03 but benefits from it.

---

## PR-05 — Booking confirmation & "My Reservations"

**Goal:** Close the loop: a confirmation with a booking reference and a page where users manage
their reservations. Adds **one small read endpoint**.

**Scope**
- Backend: add `GET /api/profile/reservations` (or `/api/reservations/mine`) returning the
  signed-in user's reservations with screening/film/cinema/seat info. Auth required; users see
  only their own.
- A derived, display-only **booking reference** (e.g. from reservation id + screening) — no
  schema change required if derived; if persisted, treat as a small migration and say so.
- Frontend: a **My Reservations** page (new SPA route) listing reservations grouped by
  screening, each with **Cancel** (confirm modal → success toast) reusing the existing cancel
  endpoint. Add the nav link for authenticated users.
- A **confirmation** state after reserving (PR-04) showing seat + reference.
- Wire the new SPA route via the **catch-all fallback** if PR retires the allow-list, otherwise
  add the route to both `App.jsx` and `Program.cs` (and note the coupling for a later cleanup).

**Out of scope:** payments; email confirmations.

**Acceptance criteria**
- New endpoint returns only the caller's reservations; 401 when unauthenticated; covered by a
  backend test.
- My Reservations shows real data, empty state, loading, and error states; cancel works and
  refreshes.
- New route reachable on direct load/refresh (SPA fallback or dual-registration verified).
- `dotnet test` green.

**Verification:** reserve a seat, see confirmation + reference, open My Reservations, cancel,
verify seat frees up; direct-URL refresh of the new route works.

**Risk:** Medium. Endpoint authorization scoping must be correct (no leaking others' bookings).

**Depends on:** PR-04 (confirmation surface); backend + frontend.

---

## PR-06 — API hardening: error contract, validation, OpenAPI

**Goal:** Standardize the API contract and make it self-documenting, without breaking existing
clients. **Authorizes adding Swashbuckle (OpenAPI) — justify in the PR; dev-only.**

**Scope**
- Adopt **ProblemDetails**-based error responses across `/api/*` while preserving fields the
  React client already reads (`message`, and `current` on concurrency conflicts). Document the
  status-code contract from [ARCHITECTURE.md §2.2](./ARCHITECTURE.md#22-standard-api-contract).
- Add a model-validation filter so invalid requests return a uniform 400.
- Add **Swagger/OpenAPI** in the Development environment only.
- Optional: extract a thin `ReservationService` (per
  [ARCHITECTURE.md §2.1](./ARCHITECTURE.md#21-layering-lightweight-not-over-engineered)) to make
  conflict translation unit-testable — only if it keeps the diff PR-sized; otherwise defer.

**Out of scope:** new endpoints, behavior changes to happy paths.

**Acceptance criteria**
- Error responses are consistent; the React client still parses messages and concurrency
  `current` payloads unchanged (verified by clicking through profile/admin conflict flows).
- Swagger UI available in Development only; not exposed in Production.
- `dotnet build -warnaserror` (if adopted) and `dotnet test` green.
- Swashbuckle addition justified in the PR description.

**Verification:** hit endpoints via Swagger; trigger a 400, 401, 403, 404, 409 and confirm
shapes; confirm the SPA still handles them.

**Risk:** Medium. Must not change response fields the SPA depends on.

**Depends on:** none hard; best after PR-05 so all endpoints exist.

---

## PR-07 — Automated testing foundation

**Goal:** Lock in correctness on the concurrency-critical paths. **Authorizes adding test
frameworks** (xUnit + `WebApplicationFactory`; Vitest + React Testing Library) — justify in PR.

**Scope**
- Backend test project (e.g. `MVC.Tests`) using `WebApplicationFactory` against a fresh
  SQLite/in-memory DB with a seeded fixture. Cover:
  - **Seat conflict → 409** (two reservations, same seat).
  - **RowVersion concurrency → 409** (stale profile + admin user update/delete).
  - **Authorization matrix** (anon/auth/admin → 200/401/403 per endpoint).
  - **Admin bootstrap** (first user becomes admin only when none exists).
  - **Cascade** (delete screening removes reservations; delete user with reservations handled).
- Frontend tests (Vitest + RTL): seat map state rendering, reserve success + 409 path, auth
  route gating, form validation/error rendering.
- Add npm `test` script.

**Out of scope:** exhaustive coverage; CI wiring (PR-08).

**Acceptance criteria**
- `dotnet test` and `npm test` pass locally and assert the behaviors above.
- Tests use disposable DBs; no reliance on a developer's `app.db`.
- New test dependencies justified in the PR.

**Verification:** run both suites from a clean checkout; confirm the 409 tests fail if the
unique constraint / RowVersion handling is removed (sanity check).

**Risk:** Low–medium. Test infra setup; isolate test DB from local `app.db`.

**Depends on:** ideally after PR-06 (stable contract), but can start after PR-01.

---

## PR-08 — Continuous integration

**Goal:** A required CI pipeline so every PR is built and tested automatically.

**Scope**
- GitHub Actions workflow (`.github/workflows/ci.yml`):
  - **backend:** `dotnet restore` → `dotnet build` → `dotnet test`.
  - **frontend:** `npm ci` → `npm run build` → `npm test`.
  - Cache NuGet + npm; run on `pull_request` and `push` to `main`.
- Optional lint/format gate (`dotnet format --verify-no-changes`, ESLint) — add only if
  configured; keep the workflow green.
- Document the CI badge/expectations in the README.

**Out of scope:** deployment automation (PR-10), secrets.

**Acceptance criteria**
- Workflow runs and passes on a clean PR; fails when a test fails.
- No secrets used; SQLite/in-memory only.
- README mentions CI.

**Verification:** open a draft PR and confirm the workflow runs green; intentionally break a
test in a scratch commit to confirm it goes red, then revert.

**Risk:** Low.

**Depends on:** PR-07 (there must be tests to run).

---

## PR-09 — UX polish: toasts, modals, auth & admin

**Goal:** Replace browser `alert`/`confirm` with product-grade notifications and dialogs, and
polish auth + admin screens. Behavior preserved.

**Scope**
- `Toast` provider/context and `Modal`/`ConfirmDialog` components
  ([DESIGN_SYSTEM.md §4](./DESIGN_SYSTEM.md#4-component-inventory)); replace `window.confirm`
  (screening/user/reservation deletes) and inline successes with toasts.
- Polish `LoginPage`/`RegisterPage`: labels, inline validation, surfaced password rules,
  friendly API-error mapping, focus management.
- Polish admin Users + Screenings management views with token styling, search, and clear
  RowVersion-conflict reconciliation using the returned `current` values.

**Out of scope:** new endpoints; auth model changes.

**Acceptance criteria**
- No remaining `window.confirm`/`alert` in primary flows; destructive actions use a focus-
  trapped confirm modal; Esc closes and focus returns.
- Auth forms validate inline and map server errors cleanly.
- Admin conflict flow shows a clear reload-and-retry path; behavior unchanged.
- Accessibility bar met for new components (focus, `aria-live`, keyboard).

**Verification:** keyboard-only run of delete-confirm and auth flows; trigger a RowVersion
conflict and reconcile; screenshots.

**Risk:** Low–medium. Modal focus management and `aria-live` correctness.

**Depends on:** PR-01; complements PR-02/04/05.

---

## PR-10 — Production readiness: provider config, security, deploy docs

**Goal:** Make the "real product" story deployable and documented, without paid services or
secrets. **Authorizes adding the PostgreSQL EF provider package** (justify; SQLite stays
default).

**Scope**
- Make the EF provider **configuration-selectable** (SQLite default; PostgreSQL when configured
  via connection string). No secrets committed; document the Postgres path in the README.
- Replace the duplicated SPA route allow-list with a **catch-all SPA fallback** in `Program.cs`
  (keep `/api/*` + MVC/Razor intact), removing the `App.jsx`/`Program.cs` coupling.
- Security headers (e.g. CSP-compatible defaults, X-Content-Type-Options, Referrer-Policy),
  review cookie/SameSite settings, and basic rate limiting on auth endpoints.
- Deployment guide in `docs/` and README: local one-server mode (unchanged) + production
  (PostgreSQL + migrations + reverse proxy); optional Dockerfile/compose if it stays small.
- A short performance/accessibility pass and a "future work / monetization" note linking
  [PRODUCT_BRIEF.md §8](./PRODUCT_BRIEF.md#8-monetization-narrative-future-documented-only).

**Out of scope:** real payments; managed/paid infra; CI deploy automation.

**Acceptance criteria**
- App still runs locally with SQLite from a clean checkout (default path unchanged).
- PostgreSQL can be enabled purely via configuration; documented; no secrets in Git.
- New SPA routes work via fallback without editing `Program.cs`; existing routes/behavior
  preserved.
- Security headers present; auth endpoints rate-limited; `dotnet test` + CI green.
- New dependency (Npgsql provider) justified in the PR.

**Verification:** clean-checkout local run (SQLite); a local Postgres run via config (if
available) or documented steps; confirm headers via browser/devtools; refresh a deep SPA route.

**Risk:** Medium. Provider switch and routing fallback must not break local default or legacy
routes.

**Depends on:** PR-06 (contract), PR-08 (CI to validate).

---

## Sequencing summary

```
PR-01 ─► PR-02 ─► PR-03 ─► PR-04 ─► PR-05 ─► PR-06 ─► PR-07 ─► PR-08 ─► PR-09 ─► PR-10
  │        │                 │                          ▲                 ▲
  └────────┴── (UI track) ───┘                          │                 │
        (tests/CI track may branch from PR-01) ─────────┴── PR-07 ────────┘
```

Hard dependencies: PR-02→01, PR-04→01, PR-08→07, PR-10→06. Others are soft/ordering.

## Open questions

1. **Film model shape:** add metadata fields to `Screening`, or introduce a reusable `Film`
   entity? (Recommendation: fields on `Screening` first; `Film` later if reuse is needed.)
2. **Reservation flow:** adopt explicit **select-then-confirm**, or keep instant reserve?
   (Recommendation: select-then-confirm — clearer and leaves a seam for future checkout.)
3. **Posters:** placeholder/generated art vs. curated clearly-licensed images? (Must avoid
   committing copyrighted binaries; default to generated/placeholder + URL field.)
4. **Booking reference:** derived (no schema) vs. persisted (small migration)? (Recommendation:
   derived first.)
5. **Legacy MVC/Razor:** keep indefinitely, or schedule a removal PR once the SPA fully covers
   every flow? (Out of scope here; flag for a future decision.)
6. **Light theme:** ship dark-only now, add a light theme later? (Recommendation: dark-only
   now.)
7. **Timezone handling:** how should `StartTime` be stored/displayed (UTC vs local)? Decide in
   PR-03 and document.

## Recommended first implementation PR

**PR-01 — Design system foundation & app shell.** It is purely visual, low-risk, adds no
dependencies and no schema/route/behavior changes, satisfies all AGENTS.md constraints, gives
the immediate "this is not a school demo" payoff, and unblocks every subsequent UI PR.
