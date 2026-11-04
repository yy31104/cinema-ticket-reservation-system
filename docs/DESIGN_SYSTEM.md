# Interface styles

The React interface uses Bootstrap components, dark surface tokens, and custom cinema/seat-map styles. [main.jsx](../ClientApp/src/main.jsx) imports Bootstrap first, then [tokens.css](../ClientApp/src/styles/tokens.css) and [styles.css](../ClientApp/src/styles.css).

## Tokens

The values below are defined in `tokens.css`; Bootstrap color variables reference the same tokens.

| Use | Token | Value |
| --- | --- | --- |
| Page background | `--color-bg` | `#0b0b0f` |
| Main surface | `--color-surface` | `#15151d` |
| Raised surface | `--color-surface-2` | `#1e1e29` |
| Primary action | `--color-primary` | `#f4b740` |
| Main text | `--color-text` | `#f5f5f7` |
| Muted text | `--color-text-muted` | `#a6a6bc` |
| Success / cancellation state | `--color-success` | `#2fb67c` |
| Error | `--color-danger` | `#e5484d` |
| Focus ring | `--color-focus-ring` | `#8fb3ff` |

The font stack starts with system fonts. Spacing tokens range from `0.25rem` to `4rem`; body text is `1rem`. The main content width is capped at `1200px` and the sticky navigation has a `4rem` minimum height.

## Pages and components

- [NavigationBar.jsx](../ClientApp/src/components/NavigationBar.jsx) uses Bootstrap's collapsing navigation and shows links for the current session/role.
- [ScreeningCard.jsx](../ClientApp/src/components/ScreeningCard.jsx) pairs a poster or title-initials fallback with showtime, cinema, optional metadata, and an availability badge.
- [StatusBadge.jsx](../ClientApp/src/components/StatusBadge.jsx) renders a text label and a colored dot. Cards derive availability from capacity minus reservation count.
- [LoadingSkeleton.jsx](../ClientApp/src/components/LoadingSkeleton.jsx) and [EmptyState.jsx](../ClientApp/src/components/EmptyState.jsx) provide the listing's loading and empty views.
- [ScreeningDetailsPage.jsx](../ClientApp/src/pages/ScreeningDetailsPage.jsx) presents film details above the seat map and renders success/error messages with live-region roles.

## Seat states

[SeatButton.jsx](../ClientApp/src/components/SeatButton.jsx) renders the seat number, visible state word, and an accessible label containing row, seat, and action. [SeatLegend.jsx](../ClientApp/src/components/SeatLegend.jsx) names the four visual states.

| State | Visible treatment | Action |
| --- | --- | --- |
| Free | Dark surface, `Open` | Select the seat |
| Selected | Amber fill, `Selected` | Confirm or clear through the selection panel |
| Reservation the user can cancel | Green border/text, `Yours` or `Cancel` | Cancel; admins can also cancel another user's reservation |
| Taken by another user | Subdued surface/text, `Taken` | Exposed with `aria-disabled`; activation has no action |
| Pending request | `Wait`, reduced opacity, wait cursor | The affected seat is disabled; pending confirmation also disables Confirm/Clear |

[SeatMap.jsx](../ClientApp/src/components/SeatMap.jsx) draws the screen above numbered rows. Rows with at least eight seats receive a center aisle. Its header reports open/taken/owned counts, and a separate panel names the selected row and seat before confirmation.

## Keyboard and layout

- The seat map uses a roving tab stop: one seat has `tabIndex=0`, and grid navigation moves that stop.
- Arrow keys move between neighboring seats and clamp at row/grid edges. Home and End move to the first/last seat of the current row.
- Enter or Space selects a free seat or cancels a cancellable reservation. The separate `Confirm reservation` button submits a selected seat.
- Taken seats remain reachable through grid navigation so their accessible labels can be read. Free seats expose selection through `aria-pressed`.
- `styles.css` supplies visible focus rings for links, forms, navigation, and seat buttons. The screen position has a visually hidden description, and the selection panel uses a polite live region.

At the `37.4375em` media breakpoint, screening cards and film-detail layouts become single-column and seat-map spacing decreases. The auditorium scrolls horizontally, retaining a `2.75rem` minimum seat width/height. `prefers-reduced-motion: reduce` shortens animations/transitions and disables smooth scrolling.
