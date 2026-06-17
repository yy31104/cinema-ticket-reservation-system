# Cinema Ticket Purchasing System

## Project Description
University EGUI Task 3-4 cinema ticket purchasing system with an ASP.NET Core backend, Entity Framework Core database access, ASP.NET Core Identity authentication, and a React frontend.

The original ASP.NET MVC/Razor implementation is still present. The Task 3-4 React application uses JSON APIs under `/api/...` and can be served by ASP.NET as a production one-server build.

## Task 3-4 Feature Checklist
- ASP.NET Core backend with JSON API endpoints
- React frontend built with Vite
- Entity Framework Core with SQLite
- Bootstrap styling
- User registration, login, logout, and profile editing
- Admin user list/edit/delete
- RowVersion concurrency handling for profile/admin user edit/delete
- Admin screening create/delete
- Deleting a screening cascades deletion of its reservations
- Screening list and screening details
- Seat grid with free/occupied/current-user reservation states
- Seat reservation and cancellation by row/seat position
- Same-seat reservation conflict handling with database unique constraint
- Production one-server mode: ASP.NET serves API and React static files

## Technologies Used
- ASP.NET Core MVC / Web API (.NET 8)
- ASP.NET Core Identity
- Entity Framework Core
- SQLite
- React
- Vite
- React Router
- Bootstrap

## Database
- Database provider: SQLite
- Local database file: `app.db`
- EF Core migrations location: `Data/Migrations`

### Main Entities
- `ApplicationUser`
- `Cinema`
- `Screening`
- `Reservation`

### Seeded Data
- Fixed list of cinemas

### Reservation Rule
- Unique constraint on `(ScreeningId, RowNumber, SeatNumber)` prevents double reservation of the same seat for the same screening.

## Known Admin/Test Accounts
`Program.cs` promotes these existing users to the `Admin` role if they are present in the database:

- `admin@example.com`
- `user@example.com`

No plaintext passwords are stored in the source code. If the local `app.db` already contains those accounts, use the password originally created for that database. Otherwise, the first registered user becomes admin when no admin users exist.

## Development Mode
Run backend and React dev server separately.

From the project root:

```bash
dotnet restore
dotnet ef database update
dotnet run
```

In another terminal:

```bash
cd ClientApp
npm install
npm run dev
```

Open the Vite URL shown in the terminal, usually:

```text
http://localhost:5173/screenings
```

The Vite dev server proxies `/api` requests to the ASP.NET backend. By default the proxy target is `http://localhost:5101`. To use another backend URL in PowerShell:

```bash
$env:VITE_API_TARGET = "https://localhost:7182"
npm run dev
```

## Production One-Server Mode
ASP.NET can serve the backend APIs, existing MVC/Razor pages, and the React production build from one server. It serves:

- MVC/Razor pages
- `/api/...` backend endpoints
- React static assets under `/app/...`
- React route fallbacks such as `/screenings`, `/profile`, and `/admin/users`

For a local one-server test without publishing, build React first, then run ASP.NET:

```bash
cd ClientApp
npm install
npm run build
cd ..
dotnet run
```

Then open one of the ASP.NET URLs shown by `dotnet run`, for example:

```text
http://localhost:5101/screenings
https://localhost:7182/screenings
```

For production publish, run this from the project root:

```bash
dotnet publish -c Release
```

The publish target automatically runs `npm ci` and `npm run build` inside `ClientApp`, then includes the generated `wwwroot/app` files in the publish output.

Run the published app from the publish folder, for example:

```bash
cd bin/Release/net8.0/publish
dotnet MVC.dll
```

Useful production one-server test URLs:

- React screening list: `/screenings`
- React login: `/login`
- React register: `/register`
- React profile: `/profile`
- React admin users: `/admin/users`
- API screening list: `/api/screenings`
- Existing MVC home page: `/Home/Index`
- Existing MVC screening list: `/Screenings/Index`
- Existing MVC profile edit page: `/Profile/Edit`
- Existing Identity login page: `/Identity/Account/Login`

The React production build output in `wwwroot/app` is generated and ignored by Git. For development, keep using `dotnet run` and `npm run dev` separately.

## Concurrency Notes

### Same Seat Conflict
Seat reservation is protected by a database-level unique constraint on `(ScreeningId, RowNumber, SeatNumber)`. Reservation creation relies on this constraint so concurrent attempts to reserve the same seat are resolved atomically by the database. If two users try to reserve the same seat at the same time, only one reservation succeeds and the other user receives a conflict message.

### User Edit/Delete Concurrency
`ApplicationUser` uses `RowVersion` as an EF Core concurrency token. Profile edit and admin user edit/delete flows send the original `RowVersion` so stale updates return `409 Conflict`. New `RowVersion` values are generated centrally in `ApplicationDbContext` before saving.

## Project Structure Overview
- `Controllers/` - MVC and API controllers
- `Models/` - Domain models, view models, and API DTOs
- `Data/` - `ApplicationDbContext` and EF Core migrations
- `Views/` - Existing Razor views
- `Areas/Identity/` - Existing Identity Razor pages
- `ClientApp/` - React frontend source
- `wwwroot/app/` - Generated React production build
- `Program.cs` - Service configuration, routing, Identity setup, React fallback, and admin role initialization
