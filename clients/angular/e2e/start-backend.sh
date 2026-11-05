#!/usr/bin/env bash
# Starts the real ASP.NET Core backend on a throwaway SQLite database for the
# Playwright smoke test, with the admin account created before the serving
# instance starts (the explicit bootstrap from #13):
#
#   1. migrate a fresh database;
#   2. boot a short-lived seed instance and register SMOKE_ADMIN_EMAIL;
#   3. start the instance the tests use with Admin:Emails listing that
#      account, so the startup bootstrap promotes it.
#
# The tests never register the admin themselves, so nothing depends on
# which account a test happens to register first.
set -euo pipefail

repo_root="$(cd "$(dirname "$0")/../../.." && pwd)"
db="${SMOKE_DB:-$repo_root/clients/angular/e2e/.smoke.db}"
port="${API_PORT:-5199}"
seed_port="${SEED_PORT:-5198}"
admin_email="${SMOKE_ADMIN_EMAIL:?set by playwright.config.ts}"
admin_password="${SMOKE_ADMIN_PASSWORD:?set by playwright.config.ts}"

rm -f "$db" "$db-shm" "$db-wal"
export ConnectionStrings__DefaultConnection="DataSource=$db;Cache=Shared"

cd "$repo_root"
dotnet tool restore > /dev/null
dotnet build MVC.csproj --configuration Debug --nologo --verbosity quiet
dotnet ef database update --project MVC.csproj --no-build --connection "DataSource=$db"

app_dll="$repo_root/bin/Debug/net8.0/MVC.dll"

# --- 2. seed instance: create the admin account, then stop -------------------
dotnet "$app_dll" --urls "http://127.0.0.1:$seed_port" > /dev/null &
seed_pid=$!
trap 'kill "$seed_pid" 2> /dev/null || true' EXIT

for _ in $(seq 1 60); do
  curl -fsS "http://127.0.0.1:$seed_port/api/screenings" > /dev/null 2>&1 && break
  sleep 1
done

curl -fsS -o /dev/null -X POST "http://127.0.0.1:$seed_port/api/auth/register" \
  -H 'Content-Type: application/json' \
  -d "{\"email\":\"$admin_email\",\"password\":\"$admin_password\",\"confirmPassword\":\"$admin_password\",\"name\":\"Smoke\",\"surname\":\"Admin\"}"

kill "$seed_pid"
wait "$seed_pid" 2> /dev/null || true
trap - EXIT

# --- 3. serving instance: promote the configured admin at startup -------------
export Admin__Emails__0="$admin_email"
exec dotnet "$app_dll" --urls "http://127.0.0.1:$port"
