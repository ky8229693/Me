export interface E2ETestData {
  email: string;
  password: string;
  todoTitle: string;
}

export const e2eAdmin = {
  email: 'e2e-admin@example.test',
  password: 'E2eAdmin123!'
} as const;

export function createE2ETestData(): E2ETestData {
  const uniqueId = `${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;

  return {
    email: `e2e-${uniqueId}@example.test`,
    password: 'E2eTest123!',
    todoTitle: `E2E todo ${uniqueId}`
  };
}
