import { describe, expect, it } from 'vitest';
import { hasPermission, permissions } from './permissions';

describe('hasPermission', () => {
  it('uses effective permissions instead of role labels', () => {
    expect(hasPermission({
      id: '1', email: 'operator@example.com', roles: ['Operator'], permissions: [permissions.rolesRead]
    }, permissions.rolesRead)).toBe(true);
    expect(hasPermission({
      id: '2', email: 'admin@example.com', roles: ['Admin'], permissions: []
    }, permissions.rolesRead)).toBe(false);
  });
});
