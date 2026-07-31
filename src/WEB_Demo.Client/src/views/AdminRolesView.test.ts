import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import PrimeVue from 'primevue/config';
import Aura from '@primeuix/themes/aura';
import AdminRolesView from './AdminRolesView.vue';
import * as roleApi from '../api/adminRoles';

const toastAdd = vi.fn();
vi.mock('primevue/usetoast', () => ({ useToast: () => ({ add: toastAdd }) }));
vi.mock('../api/adminRoles', () => ({
  getAdminRoles: vi.fn(),
  createAdminRole: vi.fn(),
  renameAdminRole: vi.fn(),
  deleteAdminRole: vi.fn(),
  getPermissionCatalog: vi.fn(),
  getRolePermissions: vi.fn(),
  updateRolePermissions: vi.fn()
}));

const getRoles = vi.mocked(roleApi.getAdminRoles);
const createRole = vi.mocked(roleApi.createAdminRole);

describe('AdminRolesView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getRoles.mockResolvedValue([
      { id: 'user', name: 'User', isSystem: true, userCount: 2 },
      { id: 'support', name: 'Support', isSystem: false, userCount: 1 }
    ]);
    createRole.mockResolvedValue({ id: 'ops', name: 'Operations', isSystem: false, userCount: 0 });
  });

  it('shows restrictions and creates a custom role', async () => {
    const wrapper = mount(AdminRolesView, {
      props: {
        currentUser: {
          id: 'admin', email: 'admin@example.com', roles: ['Admin'],
          permissions: ['roles.read', 'roles.manage', 'permissions.manage']
        }
      },
      global: { plugins: [[PrimeVue, { theme: { preset: Aura } }]] }
    });
    await flushPromises();
    expect(wrapper.text()).toContain('System');
    expect(wrapper.find('input[aria-label="Role name Support"]').element).toHaveProperty('value', 'Support');

    await wrapper.find('input[placeholder="New role name"]').setValue('Operations');
    const create = wrapper.findAll('button').find(button => button.text().includes('Create role'))!;
    await create.trigger('click');
    await flushPromises();
    expect(createRole).toHaveBeenCalledWith('Operations');
    expect(wrapper.find('input[aria-label="Role name Operations"]').element).toHaveProperty('value', 'Operations');
  });
});
