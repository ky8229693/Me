import { randomBytes } from 'node:crypto';
import { readFile } from 'node:fs/promises';
import { spawn } from 'node:child_process';
import { dirname, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const clientDirectory = resolve(scriptDirectory, '..');
const apiDirectory = resolve(clientDirectory, '../WEB_Demo.Api');

export function createDatabaseName(now = Date.now(), random = randomBytes(4).toString('hex')) {
  return `web_demo_e2e_${now.toString(36)}_${random.toLowerCase()}`;
}

export function createRunEnvironment(baseEnvironment, maintenanceConnection, databaseName) {
  const adminPassword = `E2e-${randomBytes(12).toString('base64url')}A1!`;
  return {
    ...baseEnvironment,
    ASPNETCORE_ENVIRONMENT: 'E2E',
    ASPNETCORE_URLS: 'http://localhost:5196',
    ConnectionStrings__DefaultConnection: withDatabase(maintenanceConnection, databaseName),
    E2E__MaintenanceConnection: maintenanceConnection,
    E2E_API_URL: 'http://localhost:5196',
    E2E_CLIENT_URL: 'http://127.0.0.1:5174',
    VITE_API_TARGET: 'http://localhost:5196',
    Cors__AllowedOrigins__0: 'http://127.0.0.1:5174',
    Cors__AllowedOrigins__1: 'http://localhost:5174',
    E2E_ADMIN_EMAIL: 'e2e-admin@example.test',
    E2E_ADMIN_PASSWORD: adminPassword
  };
}

export async function runE2E({
  args = [],
  baseEnvironment = process.env,
  execute = executeCommand,
  maintenanceConnection
} = {}) {
  const connection = maintenanceConnection ?? await loadMaintenanceConnection(baseEnvironment);
  const databaseName = createDatabaseName();
  const environment = createRunEnvironment(baseEnvironment, connection, databaseName);
  let primaryExitCode = 1;

  try {
    const prepareExitCode = await execute('dotnet', [
      'run', '--no-launch-profile', '--',
      'e2e-database', 'prepare',
      '--database', databaseName,
      '--admin-email', environment.E2E_ADMIN_EMAIL,
      '--password-env', 'E2E_ADMIN_PASSWORD'
    ], { cwd: apiDirectory, env: environment });

    if (prepareExitCode !== 0) {
      primaryExitCode = prepareExitCode;
      return primaryExitCode;
    }

    primaryExitCode = await execute(process.execPath, [
      resolve(clientDirectory, 'node_modules/@playwright/test/cli.js'),
      'test',
      ...args
    ], {
      cwd: clientDirectory,
      env: environment
    });
    return primaryExitCode;
  } finally {
    const cleanupExitCode = await execute('dotnet', [
      'run', '--no-launch-profile', '--',
      'e2e-database', 'cleanup',
      '--database', databaseName
    ], { cwd: apiDirectory, env: environment });

    if (primaryExitCode === 0 && cleanupExitCode !== 0) {
      process.exitCode = cleanupExitCode;
    }
  }
}

export function withDatabase(connectionString, databaseName) {
  const segments = connectionString.split(';').filter(Boolean);
  let replaced = false;
  const updated = segments.map(segment => {
    const separator = segment.indexOf('=');
    const key = separator >= 0 ? segment.slice(0, separator).trim() : '';
    if (key.toLowerCase() === 'database') {
      replaced = true;
      return `Database=${databaseName}`;
    }
    return segment;
  });
  if (!replaced) {
    updated.push(`Database=${databaseName}`);
  }
  return updated.join(';');
}

async function loadMaintenanceConnection(environment) {
  if (environment.E2E_POSTGRES_CONNECTION) {
    return environment.E2E_POSTGRES_CONNECTION;
  }

  const settingsPath = resolve(apiDirectory, 'appsettings.Development.json');
  const settings = JSON.parse(await readFile(settingsPath, 'utf8'));
  const connection = settings?.ConnectionStrings?.DefaultConnection;
  if (!connection) {
    throw new Error('Set E2E_POSTGRES_CONNECTION or configure ConnectionStrings:DefaultConnection for local development.');
  }
  return connection;
}

function executeCommand(command, args, options) {
  return new Promise((resolvePromise, reject) => {
    const child = spawn(command, args, {
      ...options,
      stdio: 'inherit'
    });
    child.once('error', reject);
    child.once('exit', code => resolvePromise(code ?? 1));
  });
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
  try {
    const exitCode = await runE2E({ args: process.argv.slice(2) });
    if (process.exitCode == null) {
      process.exitCode = exitCode;
    }
  } catch (error) {
    console.error(error instanceof Error ? error.message : error);
    process.exitCode = 1;
  }
}
