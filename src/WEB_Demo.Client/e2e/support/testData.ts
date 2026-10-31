export interface E2ETestData {
  email: string;
  password: string;
}

export const e2eAdmin = {
  email: process.env.E2E_ADMIN_EMAIL ?? 'e2e-admin@example.test',
  password: process.env.E2E_ADMIN_PASSWORD ?? ''
} as const;

export function createE2ETestData(): E2ETestData {
  const uniqueId = `${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;

  return {
    email: `e2e-${uniqueId}@example.test`,
    password: 'E2eTest123!'
  };
}
