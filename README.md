# Cinema Ticket Reservation System

![CI](https://img.shields.io/github/actions/workflow/status/yy31104/cinema-ticket-reservation-system/ci.yml?branch=main&style=flat-square&labelColor=25292E)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-8-B98952?style=flat-square&labelColor=25292E)
![SQLite](https://img.shields.io/badge/Database-SQLite-B98952?style=flat-square&labelColor=25292E)

A seat marked free can be taken before you press Confirm.

Browse screenings, sign in, and choose exact seats on an auditorium map. The ASP.NET Core API saves reservations in SQLite and reports conflicts when another reservation takes the same seat. Users can cancel their seats and edit profiles; admins manage screenings and accounts.

University course project, extended with a React seat map and API concurrency handling. Built with AI assistance.

[Run locally](#run-locally) · [Docs](#docs)

![Seat map with free, selected, and reserved seats](docs/images/seat-map.png)

Local demo with a fictional screening and sample accounts.

## How it works

1. [ScreeningsPage.jsx](ClientApp/src/pages/ScreeningsPage.jsx) loads screenings and filters them by cinema and date.
2. [AuthApiController.cs](Controllers/Api/AuthApiController.cs) registers accounts and signs them in with Identity cookies.
3. [ScreeningDetailsPage.jsx](ClientApp/src/pages/ScreeningDetailsPage.jsx) and [SeatMap.jsx](ClientApp/src/components/SeatMap.jsx) let a user select a seat, then confirm the reservation.
4. [ScreeningsApiController.cs](Controllers/Api/ScreeningsApiController.cs) checks seat bounds and saves the reservation; the database enforces one reservation per screening, row, and seat.
5. [ProfileApiController.cs](Controllers/Api/ProfileApiController.cs) checks the user's last-read version before saving a profile change.

| What goes wrong | What happens | Test |
| --- | --- | --- |
| Another user takes the same seat | The unique index rejects the second write; the API returns 409 and the UI reloads the map | [ReservationTests.cs](tests/MVC.Tests/Api/ReservationTests.cs) |
| A profile form contains an old version | The API returns 409 with the current values | [ProfileConcurrencyTests.cs](tests/MVC.Tests/Api/ProfileConcurrencyTests.cs) |
| An admin edits or deletes a user from an old form | The version check rejects the stale operation | [AdminUserConcurrencyTests.cs](tests/MVC.Tests/Api/AdminUserConcurrencyTests.cs) |

## Project layout

| Path | What | Start here |
| --- | --- | --- |
| `ClientApp/src/` | React pages, seat map, and API client | [Screening details](ClientApp/src/pages/ScreeningDetailsPage.jsx) |
| `Controllers/Api/` | Authentication, screenings, reservations, and user endpoints | [Screenings API](Controllers/Api/ScreeningsApiController.cs) |
| `Data/` | EF Core model configuration and migrations | [ApplicationDbContext.cs](Data/ApplicationDbContext.cs) |
| `Models/` | Domain records and API request/response types | [Reservation.cs](Models/Reservation.cs) |
| `tests/MVC.Tests/` | HTTP integration tests backed by SQLite | [Reservation tests](tests/MVC.Tests/Api/ReservationTests.cs) |
| `docs/` | Architecture and visual design | [Architecture](docs/ARCHITECTURE.md) |

## Run locally

Requires the .NET 8 SDK and Node.js 22.12+.

```bash
git clone https://github.com/yy31104/cinema-ticket-reservation-system.git
```

```bash
cd cinema-ticket-reservation-system
```

```bash
dotnet restore CinemaTicketReservation.sln
```

```bash
dotnet tool restore
```

```bash
dotnet ef database update --project MVC.csproj
```

```bash
npm --prefix ClientApp ci
```

```bash
npm --prefix ClientApp run build
```

```bash
dotnet run --project MVC.csproj --launch-profile http
```

Open [localhost:5101/screenings](http://localhost:5101/screenings). A new database contains three cinemas and an empty screening list. Register at `/register`; the first account becomes admin when no admin exists. Create a screening at `/admin/screenings/create`, return to the list, open it, and choose a seat before pressing Confirm.

Run backend checks from the project root. Tests take a few seconds after restore.

```bash
dotnet build CinemaTicketReservation.sln
```

```bash
dotnet test CinemaTicketReservation.sln
```

The frontend build command above is also its CI check.

## Design notes

Seat availability is a snapshot. The unique `(ScreeningId, RowNumber, SeatNumber)` index settles competing reservations at write time; a conflict clears the selection and refreshes the seat map.

`RowVersion` is an application-managed EF Core concurrency token on user records. `ApplicationDbContext.cs` replaces it on user inserts and updates; clients send the value they last read, and stale updates return 409 with current values.

ASP.NET Core serves the React build and JSON API from one process, while preserving the original MVC/Razor routes. SQLite keeps the local setup to one database file; integration tests use SQLite to exercise its actual constraints.

## Limits

- The flow ends at a confirmed reservation; checkout and payments are outside the app.
- Admin access follows local demo bootstrap rules: the first account and two legacy sample email addresses can receive the role. Review [AdminRoleService.cs](Services/AdminRoleService.cs) before hosting a shared instance.
- Screening times are stored as local wall-clock values; venues in different timezones need an explicit timezone model.

## Docs

- [Architecture and concurrency](docs/ARCHITECTURE.md)
- [Design system and seat states](docs/DESIGN_SYSTEM.md)
