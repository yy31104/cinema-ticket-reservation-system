#!/usr/bin/env bash
# Starts the real ASP.NET Core backend on a throwaway SQLite database for the
# Playwright smoke test, with the admin account created before the serving
# instance starts:
#
#   1. migrate a fresh database;
#   2. boot a short-lived seed instance, register a placeholder account first
#      (the first registration becomes admin), then register SMOKE_ADMIN_EMAIL,
#      which is therefore not an admin yet;
#   3. start the instance the tests use with Admin:Emails listing that
#      account, so only the startup bootstrap can promote it.
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

register() {
  curl -fsS -X POST "http://127.0.0.1:$seed_port/api/auth/register" \
    -H 'Content-Type: application/json' \
    -d "{\"email\":\"$1\",\"password\":\"$admin_password\",\"confirmPassword\":\"$admin_password\",\"name\":\"Smoke\",\"surname\":\"$2\"}"
}

register "placeholder-$admin_email" Placeholder > /dev/null
admin_json="$(register "$admin_email" Admin)"
case "$admin_json" in
  *'"isAdmin":true'*)
    echo "start-backend.sh: $admin_email became admin at registration; only Admin:Emails should promote it" >&2
    exit 1
    ;;
esac

kill "$seed_pid"
wait "$seed_pid" 2> /dev/null || true
trap - EXIT

# --- 3. serving instance: promote the configured admin at startup -------------
export Admin__Emails__0="$admin_email"
exec dotnet "$app_dll" --urls "http://127.0.0.1:$port"
