# Contributing

Use the .NET 8 SDK and Node.js 22.12+.

Install and prepare the local database:
```bash
dotnet restore CinemaTicketReservation.sln
dotnet tool restore
dotnet ef database update --project MVC.csproj
npm --prefix ClientApp ci
npm --prefix ClientApp run build
```
Run the app:
```bash
dotnet run --project MVC.csproj --launch-profile http
```
Check changes:
```bash
dotnet build CinemaTicketReservation.sln
dotnet test CinemaTicketReservation.sln
npm --prefix ClientApp run build
git diff --check
```

- Preserve the `v0-school-submission` tag and existing MVC/Razor and React routes.
- Keep authorization, the unique seat constraint, and user-version checks intact.
- Change schema, migrations, seed data, or dependencies only when requested.
- Keep secrets, local databases, and generated client/build output out of Git.
- Keep changes focused and run the relevant checks before delivery.
