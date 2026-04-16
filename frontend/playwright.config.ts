import { defineConfig, devices } from '@playwright/test';

const useManagedServers = process.env.PLAYWRIGHT_MANAGED_SERVERS === 'true';

export default defineConfig({
  testDir: './tests/e2e',
  timeout: 30_000,
  expect: {
    timeout: 5_000,
  },
  reporter: [['list'], ['html', { open: 'never' }]],
  webServer: useManagedServers
    ? [
        {
          command: 'npm run dev',
          url: 'https://localhost:5173',
          cwd: '.',
          ignoreHTTPSErrors: true,
          reuseExistingServer: !process.env.CI,
          timeout: 120_000,
        },
        {
          command: 'dotnet run --launch-profile https --project src\\SecondHandShop.WebApi\\SecondHandShop.WebApi.csproj',
          url: 'https://localhost:7266',
          cwd: '..',
          ignoreHTTPSErrors: true,
          reuseExistingServer: !process.env.CI,
          timeout: 120_000,
        },
      ]
    : undefined,
  use: {
    baseURL: process.env.PLAYWRIGHT_BASE_URL ?? 'https://localhost:5173',
    ignoreHTTPSErrors: true,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
