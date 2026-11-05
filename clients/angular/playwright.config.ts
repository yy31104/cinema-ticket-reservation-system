import { defineConfig, devices } from '@playwright/test';
import { smokeAdmin } from './e2e/smoke-admin';

const apiPort = Number(process.env.API_PORT ?? 5199);
const webPort = Number(process.env.WEB_PORT ?? 4300);

/**
 * Smoke test against the real stack: ASP.NET Core on a fresh SQLite file,
 * plus `ng serve` proxying /api to it. Nothing is mocked.
 */
export default defineConfig({
  testDir: './e2e',
  timeout: 30_000,
  workers: 1,
  retries: process.env.CI ? 1 : 0,
  forbidOnly: !!process.env.CI,
  reporter: process.env.CI ? [['github'], ['list']] : 'list',
  use: {
    baseURL: `http://127.0.0.1:${webPort}`,
    trace: 'retain-on-failure',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: [
    {
      command: 'bash e2e/start-backend.sh',
      url: `http://127.0.0.1:${apiPort}/api/screenings`,
      env: {
        API_PORT: String(apiPort),
        SMOKE_ADMIN_EMAIL: smokeAdmin.email,
        SMOKE_ADMIN_PASSWORD: smokeAdmin.password,
      },
      timeout: 240_000,
      reuseExistingServer: false,
      stdout: 'ignore',
      stderr: 'pipe',
    },
    {
      command: `npx ng serve --host 127.0.0.1 --port ${webPort}`,
      url: `http://127.0.0.1:${webPort}`,
      env: { API_TARGET: `http://127.0.0.1:${apiPort}` },
      timeout: 120_000,
      reuseExistingServer: false,
    },
  ],
});
