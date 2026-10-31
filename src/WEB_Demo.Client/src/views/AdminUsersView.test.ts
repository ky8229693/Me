import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import PrimeVue from 'primevue/config';
import Aura from '@primeuix/themes/aura';
import AdminUsersView from './AdminUsersView.vue';
import { i18n } from '../i18n';
import * as adminApi from '../api/adminUsers';
import * as roleApi from '../api/adminRoles';

const toastAdd = vi.fn();
vi.mock('primevue/usetoast', () => ({ useToast: () => ({ add: toastAdd }) }));
vi.mock('../api/adminUsers', () => ({
  getAdminUsers: vi.fn(),
  setAdminUserEnabled: vi.fn(),
  setAdminUserRoles: vi.fn()
}));
vi.mock('../api/adminRoles', () => ({ getAdminRoles: vi.fn() }));

const getUsersMock = vi.mocked(adminApi.getAdminUsers);
const setEnabledMock = vi.mocked(adminApi.setAdminUserEnabled);
const setRolesMock = vi.mocked(adminApi.setAdminUserRoles);
const getRolesMock = vi.mocked(roleApi.getAdminRoles);
const user = { id: 'target', email: 'target@example.com', isEnabled: true, roles: ['User'] };

function mountView() {
  return mount(AdminUsersView, {
    props: { currentUserId: 'admin' },
    global: { plugins: [i18n, [PrimeVue, { theme: { preset: Aura } }]] }
  });
}

describe('AdminUsersView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubGlobal('confirm', vi.fn(() => true));
    vi.stubGlobal('matchMedia', vi.fn(() => ({
      matches: false,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn()
    })));
    getUsersMock.mockResolvedValue({ items: [user], page: 1, pageSize: 10, totalCount: 1 });
    getRolesMock.mockResolvedValue([
      { id: 'user', name: 'User', isSystem: true, userCount: 1 },
      { id: 'admin', name: 'Admin', isSystem: true, userCount: 1 }
    ]);
    setEnabledMock.mockResolvedValue({ ...user, isEnabled: false });
    setRolesMock.mockResolvedValue({ ...user, roles: ['User', 'Admin'] });
  });

  it('renders users and updates account status', async () => {
    const wrapper = mountView();
    await flushPromises();

    expect(wrapper.text()).toContain('target@example.com');
    const disable = wrapper.findAll('button').find(button => button.text().includes('Disable'));
    await disable!.trigger('click');
    await flushPromises();

    expect(setEnabledMock).toHaveBeenCalledWith('target', false);
    expect(wrapper.text()).toContain('Disabled');
  });

  it('saves roles and reports conflicts', async () => {
    const wrapper = mountView();
    await flushPromises();
    const save = wrapper.findAll('button').find(button => button.text().includes('Save roles'));
    await save!.trigger('click');
    await flushPromises();

    expect(setRolesMock).toHaveBeenCalledWith('target', ['User']);

    setRolesMock.mockRejectedValueOnce(new Error('The last enabled Admin role cannot be removed.'));
    await save!.trigger('click');
    await flushPromises();
    expect(toastAdd).toHaveBeenCalledWith(expect.objectContaining({
      severity: 'error',
      detail: 'The last enabled Admin role cannot be removed.'
    }));
  });

  it('disables role changes when the role catalog fails to load', async () => {
    getRolesMock.mockRejectedValueOnce(new Error('Role service unavailable.'));
    const wrapper = mountView();
    await flushPromises();

    expect(wrapper.text()).toContain('Role changes are disabled.');
    const save = wrapper.findAll('button').find(button => button.text().includes('Save roles'));
    expect(save!.attributes('disabled')).toBeDefined();
  });
});
