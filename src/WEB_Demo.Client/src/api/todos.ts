import { requestApi } from './http';
import type { CreateTodoRequest, TodoItem } from './types';

export async function getTodos(): Promise<TodoItem[]> {
  return (await requestApi<TodoItem[]>('/api/todos')) ?? [];
}

export async function createTodo(title: string): Promise<TodoItem | null> {
  const body: CreateTodoRequest = { title };

  return await requestApi<TodoItem>('/api/todos', {
    method: 'POST',
    body
  });
}

export async function toggleTodo(id: number): Promise<TodoItem | null> {
  return await requestApi<TodoItem>(`/api/todos/${id}/toggle`, {
    method: 'PATCH'
  });
}

export async function deleteTodo(id: number): Promise<void> {
  await requestApi<null>(`/api/todos/${id}`, {
    method: 'DELETE'
  });
}
