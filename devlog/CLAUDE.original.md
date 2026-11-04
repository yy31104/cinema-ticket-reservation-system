@AGENTS.md

# CLAUDE.md

## Claude's Role
Claude acts as the architect, product design lead, and final reviewer for this portfolio upgrade.

Claude should define direction, review tradeoffs, critique UX, identify risks, and approve production-style changes before merge. Claude should not make broad implementation changes directly when the work can be split into PR-sized tasks for Codex.

Codex implements focused tasks, keeps diffs small, and performs deterministic verification. Claude reviews the result for coherence, quality, and portfolio readiness.

## Review Checklist

### Architecture
- Is the change scoped to one clear concern?
- Does it fit the existing ASP.NET Core, EF Core, Identity, and React structure?
- Does it avoid unnecessary abstractions and unrelated rewrites?
- Are old MVC/Razor routes and React routes considered when behavior changes?

### UX
- Is the user flow clear for browsing screenings, authentication, reservations, profile editing, and admin work?
- Are loading, empty, success, error, and conflict states handled?
- Is the UI consistent with the project's Bootstrap/React baseline until a redesign PR explicitly changes it?
- Are accessibility basics covered, including labels, focus, contrast, and keyboard use?

### Auth
- Are anonymous, authenticated, and admin-only paths handled deliberately?
- Does the first-registered-user admin fallback remain documented or intentionally replaced?
- Are API responses appropriate for unauthenticated and unauthorized requests?
- Is sensitive account information protected from non-admin users?

### Data
- Are EF Core queries, relationships, cascade rules, and validation consistent with the domain?
- Are database changes intentional, migration-backed, and documented?
- Is local SQLite data treated as disposable and excluded from Git?

### Concurrency
- Does seat reservation still rely on the unique `(ScreeningId, RowNumber, SeatNumber)` constraint?
- Are reservation conflicts surfaced clearly to users?
- Are user edit/delete concurrency tokens preserved or intentionally replaced?

### Tests
- Are meaningful backend, frontend, or manual checks included for the changed behavior?
- Are auth, reservation conflict, admin, and error paths covered when relevant?
- Are skipped tests explained with a practical reason?

### Docs
- Are README/setup instructions updated when commands, behavior, routes, or deployment steps change?
- Are new assumptions documented where future agents will need them?
- Does the PR description explain verification clearly?

### Security
- Are secrets, personal emails, local databases, and generated files excluded?
- Are dependency additions justified and reviewed?
- Are user inputs validated server-side?
- Are authorization checks enforced on the backend, not only in the UI?
