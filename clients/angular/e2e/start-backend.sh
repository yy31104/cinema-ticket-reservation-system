#!/usr/bin/env bash
# Starts the real ASP.NET Core backend on a throwaway SQLite database for the
# Playwright smoke test. The first account registered on a fresh database
# becomes Admin, which the smoke test relies on to create a screening.
set -euo pipefail

repo_root="$(cd "$(dirname "$0")/../../.." && pwd)"
db="${SMOKE_DB:-$repo_root/clients/angular/e2e/.smoke.db}"
port="${API_PORT:-5199}"

rm -f "$db" "$db-shm" "$db-wal"
export ConnectionStrings__DefaultConnection="DataSource=$db;Cache=Shared"

cd "$repo_root"
dotnet tool restore > /dev/null
dotnet ef database update --project MVC.csproj --connection "DataSource=$db"
exec dotnet run --project MVC.csproj --no-launch-profile --urls "http://127.0.0.1:$port"
