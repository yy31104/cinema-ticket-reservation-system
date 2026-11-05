# Angular client

A second front end for the same ASP.NET Core API that `ClientApp/` (React) uses. It has two pages:

- `/`: the screenings list (`GET /api/screenings`, anonymous)
- `/screenings/:id`: a seat map. It shows an inline sign-in form if you're signed out (`POST /api/auth/login`). Once you're signed in you pick a free seat and reserve it (`GET .../seats`, `POST .../reservations`).

It doesn't add or change any endpoint. All seat rules stay in `Services/ReservationService.cs` on the server: start time, per-user limit, conflicts and idempotent retry. This client only shows the `{ message }` the API returns.

## Run

Requires Node.js `^22.22.3 || ^24.15.0 || >=26` (Angular 22). [`.nvmrc`](.nvmrc) pins the tested version.

Start the backend from the repository root (it listens on `http://localhost:5101`):

```bash
dotnet run --project MVC.csproj --launch-profile http
```

Then start the Angular dev server in a second terminal:

```bash
cd clients/angular && npm ci && npm start
```

Open http://localhost:4200. `proxy.conf.mjs` forwards `/api` to the backend, so the Identity cookie is same-origin and no CORS setup is needed. Set `API_TARGET` to use a different backend URL.

## Smoke test

```bash
cd clients/angular && npx playwright install chromium && npm test
```

Playwright starts both servers. `e2e/start-backend.sh` creates a fresh SQLite file, applies migrations and runs the backend on port 5199, and `ng serve` runs on port 4300. The test seeds an admin account and a screening through the API, then drives the UI: it opens the list, follows the link to the seat map, signs in, reserves a seat, reloads to check the seat is still reserved, and checks the free-seat count on the list.
