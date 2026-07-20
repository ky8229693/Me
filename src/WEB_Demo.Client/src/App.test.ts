import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import Aura from '@primeuix/themes/aura';
// @ts-ignore: Vue single-file component declarations are handled by Vite/Vue plugin
import App from './App.vue';
import * as authApi from './api/auth';
import * as todoApi from './api/todos';
import type { TodoItem } from './api/types';
import { notifySessionExpired } from './api/authSession';
import { createTestRouter } from './test/router';

vi.mock('./api/auth', () => ({
  refreshSession: vi.fn(),
  login: vi.fn(),
  logout: vi.fn(),
  register: vi.fn()
}));

vi.mock('./api/todos', () => ({
  getTodos: vi.fn(),
  createTodo: vi.fn(),
  toggleTodo: vi.fn(),
  deleteTodo: vi.fn()
}));

vi.mock('./api/adminUsers', () => ({
  getAdminUsers: vi.fn().mockResolvedValue({ items: [], page: 1, pageSize: 10, totalCount: 0 }),
  setAdminUserEnabled: vi.fn(),
  setAdminUserRoles: vi.fn()
}));

const refreshSessionMock = vi.mocked(authApi.refreshSession);
const loginMock = vi.mocked(authApi.login);
const logoutMock = vi.mocked(authApi.logout);
const getTodosMock = vi.mocked(todoApi.getTodos);
const createTodoMock = vi.mocked(todoApi.createTodo);
const toggleTodoMock = vi.mocked(todoApi.toggleTodo);
const deleteTodoMock = vi.mocked(todoApi.deleteTodo);

const mountedWrappers: VueWrapper[] = [];

async function mountApp(): Promise<VueWrapper> {
  const router = await createTestRouter();
  const wrapper = mount(App, {
    global: {
      plugins: [
        [PrimeVue, { theme: { preset: Aura } }],
        ToastService,
        router
      ]
    }
  });
  mountedWrappers.push(wrapper);
  return wrapper;
}

function restoredSession(user: { id: string; email: string; roles: string[] }) {
  return {
    accessToken: 'restored-token', expiresAt: '2026-07-16T00:00:00Z',
    user: {
      ...user,
      permissions: user.roles.includes('Admin')
        ? ['users.read', 'users.manage', 'roles.read', 'roles.manage', 'permissions.manage']
        : []
    }
  };
}

describe('App', () => {
  beforeEach(() => {
    vi.stubGlobal(
      'ResizeObserver',
      class ResizeObserver {
        observe() {}
        unobserve() {}
        disconnect() {}
      }
    );

    getTodosMock.mockResolvedValue([]);
    refreshSessionMock.mockResolvedValue(null);
    loginMock.mockResolvedValue(null);
    logoutMock.mockResolvedValue(undefined);
    createTodoMock.mockResolvedValue(null);
    toggleTodoMock.mockResolvedValue(null);
    deleteTodoMock.mockResolvedValue(undefined);
  });

  afterEach(() => {
    for (const wrapper of mountedWrappers.splice(0)) wrapper.unmount();
    vi.clearAllMocks();
    vi.unstubAllGlobals();
  });

  it('loads todos when the dashboard is mounted', async () => {
    refreshSessionMock.mockResolvedValue(restoredSession({ id: '1', email: 'user@example.com', roles: ['User'] }));

    await mountApp();
    await flushPromises();

    expect(getTodosMock).toHaveBeenCalledOnce();
  });

  it('shows user management navigation only for Admin users', async () => {
    refreshSessionMock.mockResolvedValue(restoredSession({ id: '1', email: 'admin@example.com', roles: ['User', 'Admin'] }));
    const wrapper = await mountApp();
    await flushPromises();

    expect(wrapper.text()).toContain('User Management');

    refreshSessionMock.mockResolvedValue(restoredSession({ id: '2', email: 'user@example.com', roles: ['User'] }));
    const userWrapper = await mountApp();
    await flushPromises();
    expect(userWrapper.text()).not.toContain('User Management');
  });

  it('toggles the desktop sidebar from the navigation button', async () => {
    refreshSessionMock.mockResolvedValue(restoredSession({ id: '1', email: 'user@example.com', roles: ['User'] }));
    const wrapper = await mountApp();
    await flushPromises();

    const layout = wrapper.find('.layout-wrapper');
    const toggle = wrapper.find('button[aria-label="Toggle navigation"]');

    expect(layout.classes()).not.toContain('layout-sidebar-collapsed');
    await toggle.trigger('click');
    expect(layout.classes()).toContain('layout-sidebar-collapsed');
    await toggle.trigger('click');
    expect(layout.classes()).not.toContain('layout-sidebar-collapsed');
  });

  it('creates a todo with the entered title', async () => {
    const createdTodo: TodoItem = { id: 2, title: 'New todo', isDone: false };
    refreshSessionMock.mockResolvedValue(restoredSession({ id: '1', email: 'user@example.com', roles: ['User'] }));
    createTodoMock.mockResolvedValue(createdTodo);

    const wrapper = await mountApp();
    await flushPromises();

    await wrapper.find('input[placeholder="Add a todo item"]').setValue('New todo');
    await wrapper.find('.todo-form').trigger('submit');
    await flushPromises();

    expect(createTodoMock).toHaveBeenCalledWith('New todo');
    expect(wrapper.text()).toContain('New todo');
  });

  it('shows validation feedback for short auth passwords', async () => {
    const wrapper = await mountApp();
    await flushPromises();

    await wrapper.find('input[placeholder="name@example.com"]').setValue('user@example.com');
    await wrapper.find('input[placeholder="Password"]').setValue('1234');
    await wrapper.find('.auth-form').trigger('submit');
    await flushPromises();

    expect(loginMock).not.toHaveBeenCalled();
    expect(wrapper.text()).toContain('Password must be at least 6 characters.');
  });

  it('toggles and deletes todos through the Todo API module', async () => {
    const todo: TodoItem = { id: 1, title: 'Existing todo', isDone: false };
    refreshSessionMock.mockResolvedValue(restoredSession({ id: '1', email: 'user@example.com', roles: ['User'] }));
    getTodosMock.mockResolvedValue([todo]);
    toggleTodoMock.mockResolvedValue({ ...todo, isDone: true });

    const wrapper = await mountApp();
    await flushPromises();

    await wrapper.find('input[type="checkbox"]').setValue(true);
    await flushPromises();

    const deleteButton = wrapper.findAll('button').find(button => button.text().includes('Delete'));
    expect(deleteButton).toBeDefined();
    await deleteButton!.trigger('click');
    await flushPromises();

    expect(toggleTodoMock).toHaveBeenCalledWith(1);
    expect(deleteTodoMock).toHaveBeenCalledWith(1);
  });

  it('waits for backend logout and returns to the login view', async () => {
    refreshSessionMock.mockResolvedValue(restoredSession({ id: '1', email: 'user@example.com', roles: ['User'] }));
    const wrapper = await mountApp();
    await flushPromises();

    const logoutButton = wrapper.findAll('button').find(button => button.text().includes('Logout'));
    expect(logoutButton).toBeDefined();
    await logoutButton!.trigger('click');
    await flushPromises();

    expect(logoutMock).toHaveBeenCalledOnce();
    expect(wrapper.find('input[placeholder="name@example.com"]').exists()).toBe(true);
  });

  it('returns to the login view immediately when automatic refresh fails', async () => {
    refreshSessionMock.mockResolvedValue(restoredSession({ id: '1', email: 'user@example.com', roles: ['User'] }));
    const wrapper = await mountApp();
    await flushPromises();

    notifySessionExpired();
    await flushPromises();

    expect(wrapper.find('input[placeholder="name@example.com"]').exists()).toBe(true);
    expect(wrapper.text()).not.toContain('Dashboard');
  });
});
