# Architecture

One ASP.NET Core host serves a React interface, JSON endpoints, and the original MVC/Razor pages. EF Core stores Identity accounts and cinema data in SQLite.

## Host and routes

| File | Responsibility |
| --- | --- |
| [Program.cs](../Program.cs) | Configures Identity, EF Core, static files, API cookie responses, and route handling |
| [App.jsx](../ClientApp/src/App.jsx) | Maps the React pages and authenticated/admin routes |
| [main.jsx](../ClientApp/src/main.jsx) | Mounts React in the browser with BrowserRouter and AuthProvider |
| [vite.config.js](../ClientApp/vite.config.js) | Writes the client build to `wwwroot/app` with the `/app/` asset base; proxies `/api` during development |

`Program.cs` sends `wwwroot/app/index.html` for its explicit list of React GET routes: `/login`, `/register`, `/screenings`, `/screenings/{int}`, `/profile`, `/admin/users`, `/admin/users/{id}`, and `/admin/screenings/create`. React renders these pages in the browser. Other requests continue to API, MVC, or Razor routing; `/` serves the original MVC home page.

Application page routes are listed in both `Program.cs` and `App.jsx`. Vite's development proxy defaults to `http://localhost:5101`, with an override through `VITE_API_TARGET`.

## Data

[ApplicationDbContext.cs](../Data/ApplicationDbContext.cs) combines Identity tables with [Cinema](../Models/Cinema.cs), [Screening](../Models/Screening.cs), and [Reservation](../Models/Reservation.cs).

- A cinema defines its row count and seats per row; migrations seed three cinemas.
- A screening belongs to a cinema and holds its title, start time, and optional film metadata.
- A reservation belongs to a screening and user, with a row and seat number.
- Deleting a screening cascades its reservations. Cinema and user foreign keys use restricted deletion.

The local connection uses `app.db`; schema changes live in [Data/Migrations](../Data/Migrations). A newly migrated database starts with empty user and screening tables. `Screening.StartTime` stores local wall-clock values without timezone conversion.

## Accounts and authorization

[AuthApiController.cs](../Controllers/Api/AuthApiController.cs) handles registration, login, logout, and the current user. Identity cookies carry the session. API authorization failures return 401 or 403; MVC/Razor requests retain the normal login redirect behavior.

[AdminRoleService.cs](../Services/AdminRoleService.cs) creates the `Admin` role. At startup it promotes existing `admin@example.com` and `user@example.com` accounts; if no admin exists, it selects the first user ordered by ID. Registration grants the role to the new account when no admin exists. These are the local demo's account bootstrap rules.

[AuthContext.jsx](../ClientApp/src/auth/AuthContext.jsx) reads `/api/auth/me`. [RequireAuth.jsx](../ClientApp/src/components/RequireAuth.jsx) gates client pages; controller authorization attributes enforce access on the server.

## Reservation conflicts

[ScreeningsApiController.cs](../Controllers/Api/ScreeningsApiController.cs) serves the listing, details, seat map, creation/deletion, and reservation/cancellation endpoints. Seat-map reads and reservations require a signed-in user; screening creation/deletion requires an admin.

`POST /api/screenings/{id}/reservations` validates auditorium bounds and inserts the requested seat. The unique `(ScreeningId, RowNumber, SeatNumber)` index in `ApplicationDbContext` arbitrates duplicate reservations. A conflicting insert returns HTTP 409.

[ScreeningDetailsPage.jsx](../ClientApp/src/pages/ScreeningDetailsPage.jsx) separates selection from confirmation. After a successful reservation or a 409, it clears the selection and reloads the seat map. Owners and admins can cancel reservations.

## User edit conflicts

[ApplicationUser.RowVersion](../Models/ApplicationUser.cs) is an EF Core concurrency token with application-managed values. `ApplicationDbContext` assigns GUID-derived bytes on user inserts and updates; DTOs expose those bytes as Base64 strings.

[ProfileApiController.cs](../Controllers/Api/ProfileApiController.cs) and [UsersApiController.cs](../Controllers/Api/UsersApiController.cs) use the client's last-read value as EF's original value for an update or user deletion. Stale operations return 409. If the user still exists, the response includes its current values; a concurrent deletion produces an account-deleted message.

## Verification

Each [ApiTestBase](../tests/MVC.Tests/Infrastructure/ApiTestBase.cs) owns a [CinemaApiFactory](../tests/MVC.Tests/Infrastructure/CinemaApiFactory.cs). The factory keeps a `DataSource=:memory:` SQLite connection open, creates the schema with `EnsureCreated`, and runs requests through ASP.NET Core's in-process test host.

The suite exercises [reservation conflicts](../tests/MVC.Tests/Api/ReservationTests.cs), [profile versions](../tests/MVC.Tests/Api/ProfileConcurrencyTests.cs), [admin user versions](../tests/MVC.Tests/Api/AdminUserConcurrencyTests.cs), [authorization](../tests/MVC.Tests/Api/AuthorizationTests.cs), and screening metadata/URL validation. [CI](../.github/workflows/ci.yml) builds and tests the solution and builds the Vite client.
