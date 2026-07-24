import { defineConfig, devices } from '@playwright/test';

/**
 * Runs against an already-running stack — either `docker compose up --build` (the README's
 * quick start, serving the production build behind nginx on :4200) or, for local iteration,
 * `ng serve` + the API running standalone. This file does not start anything itself: bringing
 * up Postgres/API/frontend is an orchestration concern that belongs to whoever runs the suite
 * (a developer, or the "e2e" CI job — see .github/workflows/ci.yml).
 */
export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI ? 'github' : 'list',
  timeout: 30_000,
  use: {
    baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:4200',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure'
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }]
});
