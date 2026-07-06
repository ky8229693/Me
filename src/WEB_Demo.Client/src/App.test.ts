import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import ElementPlus from 'element-plus';
// @ts-ignore: Vue single-file component declarations are handled by Vite/Vue plugin
import App from './App.vue';
import * as authApi from './api/auth';
import * as todoApi from './api/todos';
import type { TodoItem } from './api/types';

vi.mock('./api/auth', () => ({
  getCurrentUser: vi.fn(),
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

const getCurrentUserMock = vi.mocked(authApi.getCurrentUser);
const loginMock = vi.mocked(authApi.login);
const getTodosMock = vi.mocked(todoApi.getTodos);
const createTodoMock = vi.mocked(todoApi.createTodo);
const toggleTodoMock = vi.mocked(todoApi.toggleTodo);
const deleteTodoMock = vi.mocked(todoApi.deleteTodo);

function mountApp() {
  return mount(App, {
    global: {
      plugins: [ElementPlus]
    }
  });
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
    getCurrentUserMock.mockResolvedValue(null);
    loginMock.mockResolvedValue(null);
    createTodoMock.mockResolvedValue(null);
    toggleTodoMock.mockResolvedValue(null);
    deleteTodoMock.mockResolvedValue(undefined);
  });

  afterEach(() => {
    vi.clearAllMocks();
    vi.unstubAllGlobals();
  });

  it('loads todos on mount', async () => {
    mountApp();
    await flushPromises();

    expect(getTodosMock).toHaveBeenCalledOnce();
  });

  it('creates a todo with the entered title', async () => {
    const createdTodo: TodoItem = { id: 2, title: 'New todo', isDone: false };
    createTodoMock.mockResolvedValue(createdTodo);

    const wrapper = mountApp();
    await flushPromises();

    await wrapper.find('input[placeholder="Add a todo item"]').setValue('New todo');
    await wrapper.find('.todo-form').trigger('submit');
    await flushPromises();

    expect(createTodoMock).toHaveBeenCalledWith('New todo');
    expect(wrapper.text()).toContain('New todo');
  });

  it('shows validation feedback for short auth passwords', async () => {
    const wrapper = mountApp();
    await flushPromises();

    await wrapper.find('input[placeholder="Email"]').setValue('user@example.com');
    await wrapper.find('input[placeholder="Password"]').setValue('1234');
    await wrapper.find('.auth-form').trigger('submit');
    await flushPromises();

    expect(loginMock).not.toHaveBeenCalled();
    expect(wrapper.text()).toContain('Password must be at least 6 characters.');
  });

  it('toggles and deletes todos through the Todo API module', async () => {
    const todo: TodoItem = { id: 1, title: 'Existing todo', isDone: false };
    getTodosMock.mockResolvedValue([todo]);
    toggleTodoMock.mockResolvedValue({ ...todo, isDone: true });

    const wrapper = mountApp();
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
});
