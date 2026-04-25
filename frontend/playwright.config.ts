import { defineConfig, devices } from '@playwright/test';
import { config } from 'dotenv';

// Load E2E defaults (admin credentials, API base URL) from .env.e2e;
// any env vars already set by the user or CI take precedence.
config({ path: '.env.e2e', override: false });

const useManagedServers = process.env.PLAYWRIGHT_MANAGED_SERVERS === 'true';
const useManagedBackendServer = process.env.PLAYWRIGHT_MANAGED_BACKEND === 'true';
const defaultFrontendBaseUrl = process.env.CI === 'true' ? 'http://localhost:5173' : 'https://localhost:5173';
const frontendBaseUrl = process.env.PLAYWRIGHT_BASE_URL ?? defaultFrontendBaseUrl;

if (useManagedServers) {
  process.env.VITE_TURNSTILE_SITE_KEY ??= 'journey-turnstile-site-key';
}

const frontendDevCommand =
  process.platform === 'win32'
    ? 'C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe -Command "npm.cmd run dev"'
    : 'npm run dev';

const backendDevCommand =
  process.platform === 'win32'
    ? 'dotnet run --launch-profile https --project src\\SecondHandShop.WebApi\\SecondHandShop.WebApi.csproj'
    : 'dotnet run --launch-profile https --project src/SecondHandShop.WebApi/SecondHandShop.WebApi.csproj';

export default defineConfig({
  timeout: 30_000,
  expect: {
    timeout: 5_000,
  },
  reporter: [['list'], ['html', { open: 'never' }]],
  webServer: useManagedServers
    ? [
        {
          command: frontendDevCommand,
          url: frontendBaseUrl,
          cwd: '.',
          ignoreHTTPSErrors: true,
          reuseExistingServer: !process.env.CI,
          timeout: 120_000,
        },
        ...(useManagedBackendServer
          ? [
              {
                command: backendDevCommand,
                url: 'https://localhost:7266',
                cwd: '..',
                ignoreHTTPSErrors: true,
                reuseExistingServer: !process.env.CI,
                timeout: 120_000,
              },
            ]
          : []),
      ]
    : undefined,
  use: {
    baseURL: frontendBaseUrl,
    ignoreHTTPSErrors: true,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    {
      name: 'journey',
      testDir: './tests/journey',
      testMatch: /.*\.journey\.ts/,
      use: { ...devices['Desktop Chrome'] },
    },
    {
      name: 'smoke',
      testDir: './tests/e2e',
      testMatch: /.*\.smoke\.spec\.ts/,
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
