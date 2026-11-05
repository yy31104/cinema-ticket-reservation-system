# Cinema Ticket Reservation System

Reserve specific cinema seats in a React interface backed by an ASP.NET Core API and SQLite. Users browse screenings, confirm or cancel seats, and edit profiles; admins manage screenings and accounts.

University course project, extended with a React seat map and API concurrency handling. Built with AI assistance.

![Seat map with free, selected, and reserved seats](docs/images/seat-map.png)

Local demo with a fictional screening and sample accounts.

## Reservations under contention

Seat availability is a snapshot. The unique `(ScreeningId, RowNumber, SeatNumber)` index settles competing writes; the [reservation API](Controllers/Api/ScreeningsApiController.cs) returns 409 and the [seat map](ClientApp/src/components/SeatMap.jsx) refreshes after a conflict.

Both the API and the Razor pages reserve through [one service](Services/ReservationService.cs). It refuses seats for a screening that has already started, limits each account to `Reservations:MaxSeatsPerUserPerScreening` seats per screening (default 6), and returns the existing reservation when a user retries a seat they already hold. The limit check and the insert run in one SQLite `BEGIN IMMEDIATE` transaction, so two concurrent requests from one user cannot both pass the check.

User edits carry an application-managed `RowVersion`. The [database context](Data/ApplicationDbContext.cs) replaces it on inserts and updates, and stale profile or admin requests receive the current values with a 409 response. [Integration tests](tests/MVC.Tests/Api/ReservationTests.cs) use SQLite to exercise its constraints.

## Run locally

Requires the .NET 8 SDK and Node.js 22.12+:

```bash
git clone https://github.com/yy31104/cinema-ticket-reservation-system.git
cd cinema-ticket-reservation-system
dotnet restore CinemaTicketReservation.sln
dotnet tool restore
dotnet ef database update --project MVC.csproj
npm --prefix ClientApp ci
npm --prefix ClientApp run build
dotnet run --project MVC.csproj --launch-profile http
```

Open [localhost:5101/screenings](http://localhost:5101/screenings). A new database has three cinemas and an empty screening list. Register at `/register`; the first account becomes admin when no admin exists. Create a screening at `/admin/screenings/create`, then return to the list and choose seats.

In another terminal, build and run the backend checks; tests take a few seconds after restore:

```bash
dotnet build CinemaTicketReservation.sln
dotnet test CinemaTicketReservation.sln
```

ASP.NET Core serves the React build and API from one process alongside the original MVC/Razor routes. A second client in Angular ([`clients/angular/`](clients/angular/README.md)) has a screenings list and a seat map that call the same API, plus a Playwright smoke test that runs against the real backend. Screening times are local wall-clock values, so venues in different timezones need an explicit timezone model.

The flow ends at reservation confirmation. Admin access also includes two legacy sample email addresses; review [AdminRoleService.cs](Services/AdminRoleService.cs) before sharing an instance.

See [architecture and concurrency](docs/ARCHITECTURE.md) and [seat states and keyboard controls](docs/DESIGN_SYSTEM.md) for implementation details.
