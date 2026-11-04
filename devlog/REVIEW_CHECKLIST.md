# Review Checklist — Cinema Ticket Reservation System

> Status: Planning (final-reviewer authored). The quality gate Claude applies before approving
> any PR, and the bar Codex should self-check against before requesting review. It expands the
> checklist in [CLAUDE.md](../CLAUDE.md) with the production-style and design requirements from
> the `docs/` set. Nothing here overrides [AGENTS.md](../AGENTS.md) — it operationalizes it.

## How to use

- **Codex:** before opening a PR, self-review against every relevant section and include the
  **PR report** (bottom of this doc).
- **Claude (final reviewer):** approve only when all *applicable* gates pass. Significant
  product/UX/auth/data/security changes require explicit Claude review (per AGENTS.md).
- Mark non-applicable items `N/A` with a one-line reason rather than skipping silently.

## 0. Scope & process gates (blockers)

- [ ] The change matches **exactly one** roadmap task / clear concern; no scope creep.
- [ ] No unrelated refactors, formatting churn, or drive-by changes.
- [ ] No new dependencies unless the task **explicitly authorizes** them and the PR explains why.
- [ ] No schema/migration/seed changes unless the task **explicitly authorizes** them.
- [ ] Behavior/routes (React + MVC/Razor) preserved unless the task explicitly changes them.
- [ ] `git status --short` shows only expected files; no secrets, local DBs, or generated dirs.
- [ ] Branched from `main`; `v0-school-submission` history untouched; no auto-merge.

## 1. Architecture

- [ ] Fits the existing ASP.NET Core + EF Core + Identity + React structure
      ([ARCHITECTURE.md](./ARCHITECTURE.md)); no stack replacement.
- [ ] No unnecessary abstractions; service-layer extraction (if any) is justified and minimal.
- [ ] Controllers stay HTTP-thin; business rules live in services where introduced.
- [ ] DTO boundaries preserved — EF entities are not leaked to the client.
- [ ] SPA route changes account for the `Program.cs` allow-list **and** `App.jsx` (until the
      catch-all fallback lands in PR-10).
- [ ] Significant decisions captured as an ADR entry where warranted.

## 2. UX

- [ ] Flows are clear for browse, auth, reservation, profile, and admin.
- [ ] **All states designed:** loading (skeleton), empty, success, error, conflict (409),
      unauthorized (401/403).
- [ ] No dead ends; every error is recoverable with a clear next step.
- [ ] Destructive actions confirm before acting (modal once PR-09 lands; `window.confirm`
      acceptable only until then, and flagged for replacement).
- [ ] Microcopy follows [DESIGN_SYSTEM.md §8](./DESIGN_SYSTEM.md#8-content--microcopy-guidelines)
      (verbs on buttons, human errors).
- [ ] Responsive across [DESIGN_SYSTEM.md §3.1](./DESIGN_SYSTEM.md#31-responsive-breakpoints).

## 3. Design system conformance

- [ ] Uses **tokens** from [DESIGN_SYSTEM.md §2](./DESIGN_SYSTEM.md#2-design-tokens); no stray
      hex/px outside the token file.
- [ ] Reuses shared components (Button, Field, Card, Toast, Modal, Badge, Skeleton, EmptyState,
      SeatButton) rather than re-implementing them.
- [ ] Visual hierarchy, spacing rhythm (8px), radius, and elevation match the system.
- [ ] Bootstrap is superseded only where a component intentionally replaces it.
- [ ] Screenshots (desktop + mobile) attached for any UI change.

## 4. Accessibility (blockers for UI PRs)

- [ ] Contrast meets [DESIGN_SYSTEM.md §9](./DESIGN_SYSTEM.md#9-accessibility) (text 4.5:1, UI 3:1).
- [ ] Every interactive element has a **visible focus ring**; logical tab order.
- [ ] Full keyboard operability of the changed flow; seat map is keyboard-navigable.
- [ ] Status never conveyed by **color alone** (icon/shape/text pairing).
- [ ] Labels associated with controls; errors linked via `aria-describedby`.
- [ ] Live regions correct (`aria-live` polite for toasts, assertive for errors); modal traps
      and restores focus.
- [ ] `prefers-reduced-motion` respected.

## 5. Auth

- [ ] Anonymous / authenticated / admin paths handled deliberately and tested.
- [ ] Authorization enforced **server-side** (`[Authorize]`/roles), not just in the UI.
- [ ] API returns **401** unauthenticated and **403** forbidden for `/api/*` (no login
      redirects for the SPA).
- [ ] **First-user-admin** fallback and known-admin seeding remain intact or are intentionally,
      documentedly replaced.
- [ ] Users cannot escalate privileges or act on others' data (e.g. cancel others' reservations
      unless admin; delete self blocked).
- [ ] Sensitive account info is not exposed to non-admins; new endpoints scope data to the
      caller.

## 6. Data

- [ ] EF queries, relationships, cascade rules, and validation are consistent with the domain.
- [ ] Schema changes are intentional, **migration-backed**, reversible, and documented in the PR.
- [ ] Seed changes are deliberate; no copyrighted binaries committed (posters via URL/placeholder).
- [ ] Local SQLite (`app.db*`) treated as disposable and excluded from Git.
- [ ] Migration applies cleanly on a fresh DB (`dotnet ef database update`).

## 7. Concurrency (blockers)

- [ ] Seat double-booking still relies on the unique `(ScreeningId, RowNumber, SeatNumber)`
      constraint; the reserve path still translates the violation to **409** (not 500).
- [ ] `ApplicationUser.RowVersion` optimistic concurrency preserved for profile/user edit &
      delete; stale operations return **409** with the `current` payload.
- [ ] Conflict outcomes are surfaced clearly in the UI and the map/data refreshes.
- [ ] No change weakens DB-arbitrated correctness (no app-level race introduced).

## 8. Security

- [ ] Inputs validated **server-side** (lengths, ranges, required), not only client-side.
- [ ] No secrets, personal emails, local DBs, or generated files committed
      ([AGENTS.md](../AGENTS.md) never-commit list).
- [ ] Dependency additions are justified, minimal, and reviewed.
- [ ] Error responses don't leak stack traces/internal details to clients.
- [ ] Security headers / rate limiting / cookie settings respected where in scope (PR-10).
- [ ] No new endpoint exposes data beyond the caller's authorization.

## 9. Tests

- [ ] Meaningful backend and/or frontend tests cover the changed behavior.
- [ ] Concurrency-critical paths covered when touched: **409 seat conflict**, **RowVersion
      409**, **authorization matrix**.
- [ ] Tests use disposable DBs (SQLite/in-memory), independent of a developer's `app.db`.
- [ ] `dotnet test` and `npm test` (where applicable) pass locally / in CI.
- [ ] Any skipped test has a concrete, documented reason.

## 10. Docs

- [ ] README updated when commands, behavior, routes, or deployment steps change.
- [ ] `docs/` updated when product/architecture/design/roadmap assumptions change.
- [ ] New assumptions and decisions documented for future agents.
- [ ] Migration notes included for schema changes.

## 11. Performance & reliability (lightweight)

- [ ] No obvious N+1 or unbounded queries introduced; lists are reasonable to load.
- [ ] Frontend avoids unnecessary re-fetches; loading/skeleton states prevent layout jank.
- [ ] Build output sane; no large/unintended assets committed.

## 12. Verification ran

- [ ] `dotnet build` (and `-warnaserror` if adopted) — green.
- [ ] `dotnet test` — green (or N/A with reason).
- [ ] `cd ClientApp && npm run build` — green.
- [ ] `npm test` — green (where applicable).
- [ ] Manual click-through of affected flows incl. **all states** in §2.
- [ ] Clean-checkout local run still works (SQLite default, `dotnet run` + `npm run dev`).

---

## PR report (required in every PR description)

```
## What changed
- <one or two sentences tied to the roadmap task ID, e.g. PR-03>

## Why
- <the goal / user value>

## Files changed
- <key files + a word on each>

## Commands run
- <build/test/migrate commands and their outcomes>

## Verification
- <states exercised; manual + automated; screenshots for UI>

## Risks & rollback
- <risks, blast radius, how to revert>

## Dependencies & follow-ups
- <new deps + justification; the recommended next PR>
```

## Reviewer decision

- **Approve** — all applicable gates pass; report complete.
- **Approve with nits** — minor, non-blocking; list them.
- **Request changes** — any blocker in §0, §4 (UI), §5, §7, or §8 fails, or scope creep present.
- **Needs Claude design/architecture review** — product/UX/auth/data/security-significant.
