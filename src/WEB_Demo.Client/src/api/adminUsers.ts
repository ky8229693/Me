import { requestApi } from './http';
import type { AdminUser, PagedResult } from './types';

export function getAdminUsers(
  page = 1,
  pageSize = 10,
  search = ''
): Promise<PagedResult<AdminUser> | null> {
  const query = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize)
  });
  if (search.trim()) {
    query.set('search', search.trim());
  }

  return requestApi<PagedResult<AdminUser>>(`/api/admin/users?${query}`);
}

export function setAdminUserEnabled(userId: string, isEnabled: boolean): Promise<AdminUser | null> {
  return requestApi<AdminUser>(`/api/admin/users/${encodeURIComponent(userId)}/status`, {
    method: 'PUT',
    body: { isEnabled }
  });
}

export function setAdminUserRoles(userId: string, roles: string[]): Promise<AdminUser | null> {
  return requestApi<AdminUser>(`/api/admin/users/${encodeURIComponent(userId)}/roles`, {
    method: 'PUT',
    body: { roles }
  });
}
