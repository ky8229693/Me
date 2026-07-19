import type { AuthUser } from '../api/types';

export const permissions = {
  usersRead: 'users.read',
  usersManage: 'users.manage',
  rolesRead: 'roles.read',
  rolesManage: 'roles.manage',
  permissionsManage: 'permissions.manage'
} as const;

export function hasPermission(user: AuthUser | null, permission: string): boolean {
  return user?.permissions?.includes(permission) ?? false;
}
