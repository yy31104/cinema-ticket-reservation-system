# Product Brief — Cinema Ticket Reservation System

> Status: Planning (architect-authored). This document defines the product vision and
> scope for upgrading the project from its school baseline (`v0-school-submission`) into a
> polished, production-style portfolio product. It does not change code.

## 1. Vision

Build a **premium, modern cinema seat-reservation experience** that looks and feels like a
real commercial product a cinema chain would actually ship — not a Bootstrap school demo.

A first-time visitor should land on the app and immediately believe: *"this is a real online
cinema."* They can browse what's showing with poster-led, cinematic visuals, sign up in
seconds, pick exact seats on a polished interactive seat map, and walk away with a confirmed
reservation and a booking reference.

The bar for "done" is **portfolio-grade**: the product should be convincing enough that a
reviewer (or a hiring manager) believes it could be paid for and deployed. We achieve that
through product clarity, visual quality, reliability, accessibility, and engineering rigor —
**not** by adding real payment processing.

## 2. What "users would pay for it" means here

We are **not** integrating a payment provider (explicit constraint). Instead, "worth paying
for" is expressed through *product credibility*:

- **Trust** — clear states for success, error, conflict, and empty; no dead ends.
- **Polish** — consistent design system, real film artwork, motion, responsive layout.
- **Speed** — fast browse, instant feedback on reservation actions.
- **Reliability** — seat conflicts and concurrent edits are handled correctly and visibly.
- **Accessibility** — usable by keyboard and screen reader; not color-dependent.

The monetization model is documented as a **future option** (see §8), with the codebase
structured so that a payment/checkout step *could* be slotted in later without rework. The
current product stops at a confirmed **reservation**, which is a complete, coherent product on
its own (think "reserve your seat, pay at the counter").

## 3. Target users & personas

| Persona | Goal | Primary needs |
|---|---|---|
| **Moviegoer (Guest)** | Find out what's showing | Fast, attractive browsing without forced signup |
| **Moviegoer (Registered)** | Reserve specific seats and manage bookings | Clear seat map, confirmation, "my reservations" |
| **Cinema Admin** | Manage screenings and users | Create/remove screenings, manage accounts, cancel reservations |

Design priority order: **Registered moviegoer > Guest > Admin.** The booking flow is the
heart of the product and gets the most design investment.

## 4. Core user journeys

1. **Browse (anonymous):** Land on Screenings → see poster-led cards of what's showing →
   open a screening's details (film info + showtime + auditorium). No login required to look.
2. **Sign up / sign in:** Register (first-ever user silently becomes admin) or log in →
   returned to where they were.
3. **Reserve a seat:** Open a screening → view live seat map → select free seat(s) → confirm
   → see success state + booking reference. Same-seat race with another user is resolved with
   a clear conflict message (HTTP 409), and the map refreshes.
4. **Manage bookings:** View "my reservations" → cancel a reservation → seat returns to free.
5. **Edit profile:** Update name/surname/phone → optimistic-concurrency protected (stale edit
   → 409 with a reload-and-retry prompt).
6. **Admin — screenings:** Create a screening (film + showtime + cinema), delete a screening
   (cascades its reservations with an explicit confirmation).
7. **Admin — users:** List users, edit a user's profile fields, delete a user (cannot delete
   self), all RowVersion-guarded.
8. **Admin — moderation:** Cancel any user's reservation from the seat map.

## 5. Feature scope

### 5.1 Must preserve (from baseline — do not regress)

These are the product's spine and must keep working through every PR:

- Registration / login / logout (cookie-based Identity).
- **First registered user becomes admin** when no admin exists (documented fallback).
- Profile editing with **RowVersion optimistic concurrency** (stale edit → 409).
- Admin **user management**: list, edit, delete (cannot delete self; RowVersion-guarded).
- Admin **screening create / delete** (delete cascades reservations).
- Screening **list** and **details**.
- Interactive **seat map** with free / reserved-by-me / reserved-by-other states.
- **Reservation and cancellation** by `(row, seat)`.
- **Admin cancellation** of any reservation.
- **Same-seat conflict** handled atomically via the unique
  `(ScreeningId, RowNumber, SeatNumber)` DB constraint → surfaced as **HTTP 409**.

### 5.2 New product capabilities (the upgrade)

Ordered by product value; sequenced in [ROADMAP.md](./ROADMAP.md):

- **Cinematic design system** — dark, premium theme; tokens; consistent components
  (see [DESIGN_SYSTEM.md](./DESIGN_SYSTEM.md)).
- **Poster-led screenings browsing** — card/grid layout, grouping/filtering by date & cinema,
  loading skeletons, real empty states.
- **Film metadata** — poster image, synopsis, runtime, genre, age rating (schema change).
- **Premium seat map** — screen indicator, aisles, accessible seat states, select-then-confirm
  flow, availability summary / sold-out indication.
- **Booking confirmation + "My Reservations"** — confirmation view with a booking reference;
  a page listing the signed-in user's reservations with cancel.
- **Account & admin UX polish** — modern auth forms, toast notifications, modal confirmations
  (replacing `window.confirm`/`alert`), tidy admin tables.

### 5.3 Explicitly out of scope (do not build)

- **Real payment processing / checkout / refunds.** (Hard constraint.)
- Replacing the stack (must stay ASP.NET Core + EF Core + React).
- Removing SQLite for local/demo (PostgreSQL is a *documented future* option only).
- Email delivery, SMS, push notifications, third-party SSO.
- Multi-tenant cinema chains, dynamic pricing engines, loyalty programs.
- Rewriting Git history of `v0-school-submission`.

## 6. Product principles

1. **Look like a real product, behave like one.** Every state (loading, empty, success,
   error, conflict, unauthorized) is designed, never an afterthought.
2. **Browsing is open; acting requires an account.** Lower the barrier to look, gate the
   barrier to reserve.
3. **Conflicts are a feature, not a crash.** Seat races and stale edits produce clear,
   recoverable messaging.
4. **Accessible by default.** Keyboard, focus, contrast, and non-color status cues are
   requirements, not nice-to-haves.
5. **Incremental and reversible.** Ship PR-sized, behavior-preserving changes; keep the app
   runnable locally at every step.

## 7. Success metrics (portfolio definition of success)

Because there is no live traffic, success is judged by *demonstrable quality*:

- **Flow completeness:** Every journey in §4 works end-to-end with designed states.
- **Visual credibility:** A reviewer cannot tell it apart from a commercial cinema app.
- **Reliability proof:** Automated tests cover auth, the 409 seat conflict, and RowVersion
  concurrency.
- **Accessibility:** Keyboard-navigable seat map; passes basic contrast/focus checks.
- **Engineering signal:** Green CI, clean migrations, documented architecture and decisions.
- **Runnability:** `dotnet run` + `npm run dev` works from a clean checkout per the README.

## 8. Monetization narrative (future, documented only)

To keep the "real product" story credible without violating the no-payments constraint, the
architecture leaves a clean seam for a future checkout:

- The reservation today is the terminal step. A future **"Hold → Checkout → Confirm"** flow
  could wrap it: a short-lived seat *hold*, then a payment step, then confirmation.
- Documented options (not implemented): per-screening pricing, seat tiers (standard/premium),
  a payment provider behind an interface, and order/receipt entities.
- This narrative belongs in docs and the README's "future work" section — **no paid services,
  no SDKs, no secrets** are added.

## 9. Constraints (binding)

- No payment processing; no paid/third-party services; no new secrets.
- Do not replace the stack; keep SQLite for local/demo; PostgreSQL is future-only.
- Keep the app runnable locally throughout.
- PR-sized, focused changes; no unrelated refactors; dependencies only when a task explicitly
  justifies them.
- Preserve MVC/Razor + React behavior until a task explicitly replaces it.
- Honor all rules in [AGENTS.md](../AGENTS.md) and [CLAUDE.md](../CLAUDE.md).

## 10. Open questions

See the consolidated list at the end of [ROADMAP.md](./ROADMAP.md#open-questions).
