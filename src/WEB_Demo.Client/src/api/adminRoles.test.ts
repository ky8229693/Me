import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createAdminRole, deleteAdminRole, getAdminRoles, renameAdminRole } from './adminRoles';
import { requestApi } from './http';

vi.mock('./http', () => ({ requestApi: vi.fn() }));
const requestMock = vi.mocked(requestApi);

describe('adminRoles API', () => {
  beforeEach(() => vi.clearAllMocks());

  it('uses the role administration routes', async () => {
    await getAdminRoles();
    await createAdminRole('Support');
    await renameAdminRole('role/1', 'Operations');
    await deleteAdminRole('role/1');

    expect(requestMock).toHaveBeenNthCalledWith(1, '/api/admin/roles');
    expect(requestMock).toHaveBeenNthCalledWith(2, '/api/admin/roles', { method: 'POST', body: { name: 'Support' } });
    expect(requestMock).toHaveBeenNthCalledWith(3, '/api/admin/roles/role%2F1', { method: 'PUT', body: { name: 'Operations' } });
    expect(requestMock).toHaveBeenNthCalledWith(4, '/api/admin/roles/role%2F1', { method: 'DELETE' });
  });
});
