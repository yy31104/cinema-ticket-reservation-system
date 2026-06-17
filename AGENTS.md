# AGENTS.md

## Project Purpose
This repository started as a school cinema ticket reservation project and is being upgraded into a production-style portfolio project. The tagged baseline `v0-school-submission` represents the original stable school submission.

The goal of future work is to improve reliability, maintainability, UX, documentation, and deployment readiness without losing the ability to compare changes against the school baseline.

## Tech Stack
- ASP.NET Core MVC / Web API on .NET 8
- ASP.NET Core Identity
- Entity Framework Core with SQLite
- EF Core migrations in `Data/Migrations`
- React with Vite in `ClientApp`
- React Router and Bootstrap
- Generated React production output served from `wwwroot/app`

## Non-Negotiable Rules
- Do not rewrite history for `v0-school-submission`.
- Keep work PR-sized and focused on one clear goal.
- Do not refactor unrelated code while implementing a feature or fix.
- Do not change frontend or backend behavior unless the task explicitly asks for it.
- Do not add dependencies unless the task explicitly asks for them and the PR explains why.
- Do not touch database schema, migrations, or seed data unless the task explicitly asks for it.
- Do not commit secrets, personal credentials, local databases, generated folders, or machine-specific files.
- Preserve existing MVC/Razor and React behavior until a task explicitly replaces or removes it.
- Prefer minimal, readable changes over broad optimization passes.
- Do not automatically merge PRs.

## Safe Commands
These commands are safe for routine inspection and verification:

```bash
git status --short
git diff
git diff --staged
git log --oneline --decorate -5
rg "search text"
rg --files
dotnet restore
dotnet build
dotnet test
```

Frontend commands are safe only when dependencies already exist or the task explicitly allows installing them:

```bash
cd ClientApp
npm run dev
npm run build
npm test
```

Ask before running commands that install packages, alter the database, publish artifacts, rewrite Git history, delete files, or push to remotes.

## Definition of Done
- The change matches the task and does not include unrelated refactors.
- The app behavior changed only where intended.
- Relevant tests or manual verification were run, or skipped with a clear reason.
- `git diff` was reviewed for accidental generated files, secrets, and noisy formatting.
- Documentation was updated when behavior, setup, or workflow changed.
- `git status --short` shows only expected files.
- The PR description explains what changed, why, and how it was verified.

## PR Workflow
- Start from `main` after confirming the baseline tag exists.
- Create a branch with a clear name, for example `docs/ai-collaboration-rules` or `fix/reservation-conflict`.
- Keep each PR small enough to review in one sitting.
- Include screenshots for UI changes.
- Include migration notes for intentional database changes.
- Request architecture/design review from Claude for significant product, UX, auth, data, or security changes.
- Let Codex implement PR-sized tasks and run deterministic verification.
- Do not merge automatically; wait for review and explicit approval.

## Files That Must Never Be Committed
- `app.db`
- `app.db-shm`
- `app.db-wal`
- `.env`
- `.env.*`
- real secret-bearing `appsettings.*.json` overrides
- `node_modules/`
- `ClientApp/node_modules/`
- `bin/`
- `obj/`
- `wwwroot/app/`
- `.vs/`
- `.idea/`
- `.DS_Store`
- local publish output, logs, caches, and machine-specific editor files
