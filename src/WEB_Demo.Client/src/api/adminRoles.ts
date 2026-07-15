import { requestApi } from './http';
import type { AdminRole } from './types';

export function getAdminRoles(): Promise<AdminRole[] | null> {
  return requestApi<AdminRole[]>('/api/admin/roles');
}

export function createAdminRole(name: string): Promise<AdminRole | null> {
  return requestApi<AdminRole>('/api/admin/roles', { method: 'POST', body: { name } });
}

export function renameAdminRole(roleId: string, name: string): Promise<AdminRole | null> {
  return requestApi<AdminRole>(`/api/admin/roles/${encodeURIComponent(roleId)}`, {
    method: 'PUT',
    body: { name }
  });
}

export function deleteAdminRole(roleId: string): Promise<AdminRole | null> {
  return requestApi<AdminRole>(`/api/admin/roles/${encodeURIComponent(roleId)}`, { method: 'DELETE' });
}
