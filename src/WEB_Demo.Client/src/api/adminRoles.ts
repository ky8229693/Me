import { requestApi } from './http';
import type { AdminRole, PermissionInfo, RolePermissions } from './types';

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

export function getPermissionCatalog(): Promise<PermissionInfo[] | null> {
  return requestApi<PermissionInfo[]>('/api/admin/permissions');
}

export function getRolePermissions(roleId: string): Promise<RolePermissions | null> {
  return requestApi<RolePermissions>(`/api/admin/roles/${encodeURIComponent(roleId)}/permissions`);
}

export function updateRolePermissions(roleId: string, permissions: string[]): Promise<RolePermissions | null> {
  return requestApi<RolePermissions>(`/api/admin/roles/${encodeURIComponent(roleId)}/permissions`, {
    method: 'PUT', body: { permissions }
  });
}
