import { defineConfig, devices } from '@playwright/test';

const isCI = Boolean(process.env.CI);
const apiUrl = process.env.E2E_API_URL ?? 'http://localhost:5196';
const clientUrl = process.env.E2E_CLIENT_URL ?? 'http://127.0.0.1:5174';

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  forbidOnly: isCI,
  retries: isCI ? 2 : 0,
  workers: 1,
  reporter: [['list'], ['html', { open: 'never', outputFolder: 'playwright-report' }]],
  outputDir: 'test-results',
  timeout: 30_000,
  expect: {
    timeout: 5_000
  },
  use: {
    baseURL: clientUrl,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure'
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] }
    }
  ],
  webServer: [
    {
      command: 'dotnet run --no-launch-profile',
      cwd: '../WEB_Demo.Api',
      url: `${apiUrl}/api/health`,
      reuseExistingServer: false,
      timeout: 120_000
    },
    {
      command: 'npm run dev -- --host 127.0.0.1 --port 5174 --strictPort',
      cwd: '.',
      url: clientUrl,
      reuseExistingServer: false,
      timeout: 120_000
    }
  ]
});
