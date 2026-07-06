import { afterEach, describe, expect, it, vi } from 'vitest';
import { requestApi } from './http';
import { ApiError } from './types';

function mockApiResponse<T>(response: {
  ok: boolean;
  status: number;
  body: T;
}) {
  return {
    ok: response.ok,
    status: response.status,
    json: vi.fn().mockResolvedValue(response.body)
  } as unknown as Response;
}

describe('requestApi', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('returns typed data from a successful API envelope', async () => {
    const todo = { id: 1, title: 'Write tests', isDone: false };
    const fetchMock = vi.fn().mockResolvedValue(
      mockApiResponse({
        ok: true,
        status: 200,
        body: {
          success: true,
          message: null,
          data: todo,
          errorCode: null
        }
      })
    );
    vi.stubGlobal('fetch', fetchMock);

    await expect(requestApi<typeof todo>('/api/todos/1')).resolves.toEqual(todo);
  });

  it('throws an API error with the backend message for failed HTTP responses', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      mockApiResponse({
        ok: false,
        status: 500,
        body: {
          success: false,
          message: 'Server failed.',
          data: null,
          errorCode: 'internal-server-error'
        }
      })
    );
    vi.stubGlobal('fetch', fetchMock);

    await expect(requestApi('/api/todos')).rejects.toMatchObject({
      message: 'Server failed.',
      status: 500,
      errorCode: 'internal-server-error'
    });
  });

  it('throws an API error with the backend message for unsuccessful envelopes', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      mockApiResponse({
        ok: true,
        status: 200,
        body: {
          success: false,
          message: 'Todo was not found.',
          data: null,
          errorCode: 'todo-not-found'
        }
      })
    );
    vi.stubGlobal('fetch', fetchMock);

    await expect(requestApi('/api/todos/999')).rejects.toMatchObject({
      message: 'Todo was not found.',
      status: 200,
      errorCode: 'todo-not-found'
    });
  });

  it('sets JSON content type when a request body is provided', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      mockApiResponse({
        ok: true,
        status: 200,
        body: {
          success: true,
          message: null,
          data: { id: 2, title: 'Created todo', isDone: false },
          errorCode: null
        }
      })
    );
    vi.stubGlobal('fetch', fetchMock);

    await requestApi('/api/todos', {
      method: 'POST',
      body: { title: 'Created todo' }
    });

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(init.body).toBe(JSON.stringify({ title: 'Created todo' }));
    expect(init.headers).toBeInstanceOf(Headers);
    expect((init.headers as Headers).get('Content-Type')).toBe('application/json');
  });
});
