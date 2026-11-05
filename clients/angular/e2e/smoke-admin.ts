// The admin account the smoke backend pre-creates before it starts serving.
// Shared by playwright.config.ts (passed to start-backend.sh) and the spec.
// Throwaway credentials for a throwaway SQLite file; not a secret.
export const smokeAdmin = {
  email: process.env['SMOKE_ADMIN_EMAIL'] ?? 'smoke-admin@example.test',
  password: process.env['SMOKE_ADMIN_PASSWORD'] ?? 'Smoke-Admin-0!',
};
