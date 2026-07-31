import { afterEach, describe, expect, it, vi } from 'vitest';
import { getAdminUsers, setAdminUserEnabled, setAdminUserRoles } from './adminUsers';

function response(data: unknown) {
  return {
    ok: true,
    status: 200,
    text: vi.fn().mockResolvedValue(JSON.stringify({ success: true, message: null, data, errorCode: null }))
  } as unknown as Response;
}

describe('admin users API', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    window.localStorage.clear();
  });

  it('queries a searched page through the shared client', async () => {
    const fetchMock = vi.fn().mockResolvedValue(response({ items: [], page: 2, pageSize: 25, totalCount: 0 }));
    vi.stubGlobal('fetch', fetchMock);

    await getAdminUsers(2, 25, 'admin@example.com');

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/admin/users?page=2&pageSize=25&search=admin%40example.com',
      expect.objectContaining({ body: undefined })
    );
  });

  it('updates status and roles with PUT requests', async () => {
    const user = { id: 'u1', email: 'user@example.com', isEnabled: true, roles: ['User'] };
    const fetchMock = vi.fn().mockResolvedValue(response(user));
    vi.stubGlobal('fetch', fetchMock);

    await setAdminUserEnabled('u1', false);
    await setAdminUserRoles('u1', ['User', 'Admin']);

    expect(fetchMock).toHaveBeenNthCalledWith(1, '/api/admin/users/u1/status', expect.objectContaining({
      method: 'PUT',
      body: JSON.stringify({ isEnabled: false })
    }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, '/api/admin/users/u1/roles', expect.objectContaining({
      method: 'PUT',
      body: JSON.stringify({ roles: ['User', 'Admin'] })
    }));
  });
});
