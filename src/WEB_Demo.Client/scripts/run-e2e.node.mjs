import assert from 'node:assert/strict';
import test from 'node:test';
import { createDatabaseName, createRunEnvironment, runE2E, withDatabase } from './run-e2e.mjs';

test('creates unique allowlisted database names', () => {
  const first = createDatabaseName(1_000, 'abcd1234');
  const second = createDatabaseName(1_001, 'efgh5678');

  assert.match(first, /^web_demo_e2e_[a-z0-9_]+$/);
  assert.notEqual(first, second);
});

test('builds isolated E2E environment without changing the base environment', () => {
  const base = { CI: 'true' };
  const environment = createRunEnvironment(base, 'Host=localhost;Database=web_demo;Username=user', 'web_demo_e2e_test12345');

  assert.equal(base.ASPNETCORE_ENVIRONMENT, undefined);
  assert.equal(environment.ASPNETCORE_ENVIRONMENT, 'E2E');
  assert.match(environment.ConnectionStrings__DefaultConnection, /Database=web_demo_e2e_test12345/);
  assert.equal(environment.E2E_API_URL, 'http://localhost:5196');
  assert.equal(environment.Cors__AllowedOrigins__0, 'http://127.0.0.1:5174');
  assert.ok(environment.E2E_ADMIN_PASSWORD);
});

test('replaces or appends a database in a connection string', () => {
  assert.equal(
    withDatabase('Host=localhost;Database=web_demo;Username=user', 'web_demo_e2e_test12345'),
    'Host=localhost;Database=web_demo_e2e_test12345;Username=user'
  );
  assert.equal(
    withDatabase('Host=localhost;Username=user', 'web_demo_e2e_test12345'),
    'Host=localhost;Username=user;Database=web_demo_e2e_test12345'
  );
});

test('runs cleanup after successful Playwright execution', async () => {
  const calls = [];
  const exitCode = await runE2E({
    maintenanceConnection: 'Host=localhost;Database=postgres;Username=user',
    execute: async (command, args) => {
      calls.push([command, args]);
      return 0;
    }
  });

  assert.equal(exitCode, 0);
  assert.equal(calls.length, 3);
  assert.equal(calls[0][1][4], 'prepare');
  assert.match(calls[1][1][0], /@playwright[\\/]test[\\/]cli\.js$/);
  assert.equal(calls[2][1][4], 'cleanup');
});

test('preserves Playwright failure and still runs cleanup', async () => {
  const calls = [];
  const exitCode = await runE2E({
    maintenanceConnection: 'Host=localhost;Database=postgres;Username=user',
    execute: async (command, args) => {
      calls.push([command, args]);
      return calls.length === 2 ? 7 : 0;
    }
  });

  assert.equal(exitCode, 7);
  assert.equal(calls.length, 3);
  assert.equal(calls[2][1][4], 'cleanup');
});

test('runs cleanup when prepare fails', async () => {
  const calls = [];
  const exitCode = await runE2E({
    maintenanceConnection: 'Host=localhost;Database=postgres;Username=user',
    execute: async (command, args) => {
      calls.push([command, args]);
      return calls.length === 1 ? 5 : 0;
    }
  });

  assert.equal(exitCode, 5);
  assert.equal(calls.length, 2);
  assert.equal(calls[1][1][4], 'cleanup');
});
