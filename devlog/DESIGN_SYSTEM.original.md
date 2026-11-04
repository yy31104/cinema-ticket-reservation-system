# Design System — Cinema Ticket Reservation System

> Status: Planning (architect / product-design-lead authored). Defines the visual language and
> component standards for the premium upgrade. This document does not change code; it is the
> reference the UI PRs in [ROADMAP.md](./ROADMAP.md) implement against.

## 1. Brand & art direction

**Concept:** *A modern cinema after dark.* Deep, near-black surfaces like a darkened
auditorium, warm amber "marquee" light for primary actions, and film posters as the hero
content. The feel is **premium, focused, and cinematic** — closer to a streaming/booking app
(Netflix, MUBI, modern multiplex apps) than a Bootstrap admin template.

Tone of voice: confident, concise, friendly. Microcopy uses plain language
("Reserve seat", "Your reservation", "Seat already taken"), never jargon.

**Design pillars**

1. **Posters first.** Artwork and showtimes carry the page; chrome recedes.
2. **One accent, used sparingly.** Amber means "primary action / your thing." Overuse kills it.
3. **Dark by default.** A single, well-tuned dark theme (light theme is optional future work).
4. **Quiet motion.** Subtle, fast transitions; nothing bouncy or slow.
5. **Accessible always.** Contrast, focus, keyboard, and non-color cues are non-negotiable.

## 2. Design tokens

Implement as **CSS custom properties** on `:root` (single source of truth), layered above
Bootstrap. Component PRs consume tokens, never hard-coded hex. Values below are the target
palette; exact values may be nudged during implementation but **must keep the stated contrast
guarantees** (§9).

### 2.1 Color — surfaces & text (dark theme)

| Token | Value | Role |
|---|---|---|
| `--color-bg` | `#0B0B0F` | App background (auditorium ink) |
| `--color-surface` | `#15151D` | Cards, nav, panels |
| `--color-surface-2` | `#1E1E29` | Raised elements, inputs, hover rows |
| `--color-surface-3` | `#262633` | Popovers, menus, modals |
| `--color-border` | `#2B2B3A` | Hairline borders / dividers |
| `--color-border-strong` | `#3A3A4D` | Input borders, focus-adjacent edges |
| `--color-text` | `#F5F5F7` | Primary text |
| `--color-text-muted` | `#A6A6BC` | Secondary text, captions |
| `--color-text-subtle` | `#6E6E85` | Disabled / tertiary |

### 2.2 Color — brand & semantic

| Token | Value | On-color text | Role |
|---|---|---|---|
| `--color-primary` | `#F4B740` | `#0B0B0F` | Primary actions (marquee amber) |
| `--color-primary-hover` | `#FFC75A` | `#0B0B0F` | Primary hover |
| `--color-primary-press` | `#E0A52E` | `#0B0B0F` | Primary active/pressed |
| `--color-on-primary` | `#0B0B0F` | — | Text/icon on amber |
| `--color-success` | `#2FB67C` | `#04140D` | Success, "your reservation" |
| `--color-danger` | `#E5484D` | `#1A0506` | Errors, destructive actions |
| `--color-warning` | `#E2A93B` | `#1A1203` | Warnings (distinct from primary use) |
| `--color-info` | `#5B8DEF` | `#04101F` | Informational, links |
| `--color-focus-ring` | `#8FB3FF` | — | Visible focus outline |

> Amber is reserved for **primary intent** (CTAs, your-own items). Use `--color-warning` for
> caution states so warnings don't get confused with primary buttons.

### 2.3 Seat-map state tokens

Seat status must be distinguishable by **fill + border + icon/shape**, never color alone.

| Seat state | `--seat-*` fill | Border | Text/Icon | Cue (non-color) |
|---|---|---|---|---|
| Available | transparent / `--color-surface-2` | `--color-border-strong` | `--color-text-muted` | seat number only |
| Hover (available) | `rgba(244,183,64,.14)` | `--color-primary` | `--color-text` | slight lift |
| Selected (pre-confirm) | `--color-primary` | `--color-primary-press` | `--color-on-primary` | filled + ✓ on confirm step |
| Your reservation | `rgba(47,182,124,.18)` | `--color-success` | `--color-success` | ✓ check glyph + "Mine" |
| Reserved (others) | `--color-surface` | `--color-border` | `--color-text-subtle` | ✕/lock glyph, dimmed, `disabled` |
| Screen indicator | gradient sliver | — | label "SCREEN" | curved/elevated bar atop the map |

### 2.4 Typography

- **Primary typeface:** system UI stack (no new font dependency by default):
  `-apple-system, "Segoe UI", Roboto, Inter, system-ui, sans-serif`.
  *(A web font such as Inter/Sora may be added only by an explicit task with justification.)*
- **Scale** (rem, 16px base):

| Token | Size / line-height | Weight | Use |
|---|---|---|---|
| `--font-display` | 2.5 / 1.1 | 700 | Hero / page hero titles |
| `--font-h1` | 2.0 / 1.15 | 700 | Page titles |
| `--font-h2` | 1.5 / 1.25 | 600 | Section headers |
| `--font-h3` | 1.25 / 1.3 | 600 | Card titles |
| `--font-body` | 1.0 / 1.5 | 400 | Body text |
| `--font-small` | 0.875 / 1.45 | 400 | Secondary text |
| `--font-caption` | 0.75 / 1.4 | 500 | Labels, badges, metadata |

- Numerals in the seat map and showtimes use `font-variant-numeric: tabular-nums`.

### 2.5 Spacing, radius, elevation, motion

| Group | Tokens |
|---|---|
| **Spacing** (4px base) | `--space-1:4px --space-2:8px --space-3:12px --space-4:16px --space-5:24px --space-6:32px --space-8:48px --space-10:64px` |
| **Radius** | `--radius-sm:6px --radius-md:10px --radius-lg:16px --radius-pill:999px` |
| **Elevation** | `--shadow-1:0 1px 2px rgba(0,0,0,.4) --shadow-2:0 6px 20px rgba(0,0,0,.45) --shadow-3:0 16px 40px rgba(0,0,0,.55)` |
| **Motion** | `--ease-standard:cubic-bezier(.2,.0,.2,1) --dur-fast:120ms --dur-base:200ms --dur-slow:320ms` |
| **Layout** | `--container-max:1200px --nav-height:64px` |

Respect `prefers-reduced-motion`: disable non-essential transitions/animations when set.

## 3. Layout principles

- **Centered container** at `--container-max` with responsive gutters; full-bleed hero allowed
  on the screenings landing.
- **8px spacing rhythm** everywhere; vertical sections separated by `--space-6`/`--space-8`.
- **Content hierarchy:** hero → primary content grid → secondary info. Chrome (nav/footer)
  is quiet and consistent.
- **Cards over tables** for browsing (posters); tables remain acceptable for **admin** data
  views, but restyled with token colors, zebra rows, and proper empty states.
- **Sticky top nav** (`--nav-height`), translucent dark surface, brand left, actions right.

### 3.1 Responsive breakpoints

| Name | Min width | Screenings grid | Seat map |
|---|---|---|---|
| `sm` | 0–599 | 1 col | horizontal scroll, larger touch targets |
| `md` | 600–899 | 2 cols | fit-to-width where possible |
| `lg` | 900–1199 | 3 cols | centered, comfortable |
| `xl` | 1200+ | 4 cols | centered, max width |

Touch targets ≥ 44×44px; seat buttons keep a comfortable minimum tap size on mobile.

## 4. Component inventory

Build these as small, token-driven, reusable components. Keep Bootstrap as the grid/utility
baseline; supersede individual Bootstrap pieces only where a component below replaces them.

| Component | States / variants | Notes |
|---|---|---|
| **Button** | primary, secondary (outline), ghost, danger; sizes sm/md; loading; disabled | Min 44px tap height; visible focus ring; loading shows spinner + keeps label width |
| **Field** (input/select/textarea) | default, focus, error, disabled; with label + help + error text | Labels always present (no placeholder-only); error text via `aria-describedby` |
| **Card** | screening/poster card, info panel | Poster aspect 2:3; hover lift on interactive cards |
| **Badge / Tag** | genre, age rating, "Sold out", "Few seats left", role=Admin | Caption type; semantic color where meaningful |
| **SeatButton** | states per §2.3 | `role` button, `aria-pressed`/`aria-disabled`, label "Row R seat S — <status>" |
| **Seat legend** | static | Mirrors seat states with icon + label |
| **Toast** | success, error, info; auto-dismiss + manual close | Replaces `alert`; `aria-live=polite` (assertive for errors) |
| **Modal / ConfirmDialog** | confirm (destructive), informational | Replaces `window.confirm`; focus trap; Esc to close; returns focus |
| **Alert / inline message** | success, danger, info, warning | For form-level and page-level messages |
| **Skeleton** | card, list row, seat map | Shown during loading instead of bare "Loading…" text |
| **EmptyState** | no screenings, no reservations, no results | Icon + headline + supporting line + optional CTA |
| **Nav bar** | guest, authenticated, admin | Brand, primary links, account menu/logout, responsive collapse |
| **Pagination / filter bar** | date filter, cinema filter, search | Client-side first; server-side if data grows |

## 5. Key screen specifications

### 5.1 Screenings (landing)

- Optional **hero** featuring a highlighted/next screening (poster backdrop + title + CTA).
- **Responsive poster-card grid** (§3.1). Each card: poster, film title, cinema, showtime,
  availability badge ("Few seats left" / "Sold out" derived from reservation count vs capacity).
- **Filter bar**: by date and cinema; optional title search (client-side over loaded data).
- States: skeleton grid while loading; `EmptyState` when none; error alert with retry.
- Admin: a clearly separated "Create screening" action and per-card delete with confirm modal.

### 5.2 Screening details

- Two-column on `lg+`: poster + film metadata (synopsis, runtime, genre, age rating, cinema,
  showtime) on one side; seat map / reservation panel on the other. Single column on small.
- Anonymous users see details + a clear "Sign in to choose seats" prompt (preserves current
  behavior where the seat map requires auth).

### 5.3 Seat map (signature component)

- **Screen indicator** at the top (curved/elevated "SCREEN" bar) to orient the user.
- Seats grouped into rows with row labels; an **aisle gap** for readability on wider rooms.
- States per §2.3; reserved-by-others are visibly disabled with a non-color cue.
- **Select-then-confirm** flow: tap a free seat to *select* (amber), see a summary
  ("Row 4, Seat 7"), then **Confirm reservation**. This makes the action deliberate and gives
  a natural home for a future checkout step. (Direct-reserve remains acceptable as a fallback
  if a PR keeps it simpler — the PR must state which.)
- On success: success toast + the seat flips to "Your reservation"; the map refreshes.
- On **409 conflict**: error toast "That seat was just taken", auto-refresh the map.
- Availability summary (e.g. "38 of 96 seats available").

### 5.4 My reservations

- List of the signed-in user's reservations grouped by screening (film, showtime, cinema,
  seat, booking reference), each with a **Cancel** action (confirm modal) → success toast.
- `EmptyState` with a CTA back to Screenings when there are none.

### 5.5 Auth (login / register)

- Centered card on a quiet cinematic backdrop; clear labels, inline validation, password rules
  surfaced; primary button full-width; link to the alternate flow.
- Friendly error mapping from API messages; never expose raw stack/JSON.

### 5.6 Admin

- Users: restyled token-based table; search; edit/delete with confirm modal; RowVersion
  conflicts surfaced as a clear "reload latest and retry" message (uses returned `current`).
- Screenings: a management view (list + delete + create) consistent with the public look.

## 6. Iconography & imagery

- **Icons:** a single consistent inline-SVG set (e.g. a small hand-rolled set or a
  permissively licensed pack added only by an explicit task). Icons are decorative unless they
  convey status, in which case they pair with text/`aria-label`.
- **Posters:** stored as image URLs in film metadata. Always render with an explicit
  aspect-ratio box and a graceful **fallback** (gradient + title initials) when missing.
- **No autoplaying video**; no heavy background media that hurts performance.

## 7. Theming implementation approach

- Define all tokens in one place (e.g. `ClientApp/src/styles/tokens.css`) and import once.
- Map a few Bootstrap CSS variables to tokens where practical so existing Bootstrap components
  inherit the theme during transition; replace Bootstrap components with custom ones only as
  specific PRs require.
- **Do not** introduce a CSS-in-JS or large styling dependency unless an explicit task
  justifies it. CSS custom properties + scoped CSS are sufficient.
- Keep the **first design PR purely visual** (tokens + shell + restyle), with **no behavior or
  route changes**, so it is low-risk and reviewable.

## 8. Content & microcopy guidelines

- Buttons are verbs: "Reserve seat", "Confirm reservation", "Cancel", "Create screening".
- Errors are human and actionable: "That seat was just taken — pick another."
- Empty states reassure and guide: "No screenings yet. Check back soon."
- Dates/times use the user's locale (`Intl.DateTimeFormat`, already in use); be explicit about
  timezone handling decisions in the relevant PR.

## 9. Accessibility (requirements, not suggestions)

- **Contrast:** body text ≥ **4.5:1**; large text and UI component boundaries ≥ **3:1**.
  The palette in §2 targets this on `--color-bg`/`--color-surface`; verify each pairing during
  implementation and adjust tokens (not component code) if a pair falls short.
- **Focus:** every interactive element has a **visible focus ring** (`--color-focus-ring`);
  never remove outlines without an equivalent. Logical tab order; modal focus trap + restore.
- **Keyboard:** the entire booking flow is operable without a mouse. The **seat map** is a
  keyboard-navigable grid (arrow-key roving tabindex or sequential focus), with Enter/Space to
  select and clear labels.
- **Screen readers:** seat buttons expose "Row R, seat S, <status>"; status regions use
  `aria-live` (toasts polite, errors assertive); icons that carry meaning have `aria-label`.
- **Non-color status:** seat states, badges, and form validation never rely on color alone
  (pair with icon/shape/text).
- **Motion:** honor `prefers-reduced-motion`.
- **Forms:** programmatic `label`↔control association; errors linked via `aria-describedby`;
  required fields marked accessibly.

## 10. Definition of done (design)

A UI PR is design-complete when:

- It consumes **tokens** (no stray hex/px outside the token file).
- All relevant **states** are present: loading (skeleton), empty, success, error, conflict,
  unauthorized.
- It meets the **accessibility** bar in §9 (keyboard + focus + contrast + non-color cues).
- It is **responsive** across the §3.1 breakpoints.
- It **preserves behavior/routes** unless the task explicitly changes them.
- Screenshots (desktop + mobile) are attached to the PR.
