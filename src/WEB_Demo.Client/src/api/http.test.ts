import { afterEach, describe, expect, it, vi } from 'vitest';
import { requestApi } from './http';
import { ApiError } from './types';
import { clearAuthToken, setAuthToken } from './authToken';
import { onSessionExpired } from './authSession';

function mockApiResponse<T>(response: {
  ok: boolean;
  status: number;
  body: T | string | null;
}) {
  const body = typeof response.body === 'string'
    ? response.body
    : response.body === null
      ? ''
      : JSON.stringify(response.body);

  return {
    ok: response.ok,
    status: response.status,
    text: vi.fn().mockResolvedValue(body)
  } as unknown as Response;
}

describe('requestApi', () => {
  afterEach(() => {
    window.localStorage.clear();
    clearAuthToken();
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

  it('throws a friendly API error when fetch cannot connect', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')));

    await expect(requestApi('/api/auth/login')).rejects.toMatchObject({
      message: 'Unable to connect to the API. Please make sure the backend is running.',
      status: 0,
      errorCode: null
    });
  });

  it('throws a friendly API error for empty failed responses', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      mockApiResponse({
        ok: false,
        status: 502,
        body: null
      })
    );
    vi.stubGlobal('fetch', fetchMock);

    await expect(requestApi('/api/auth/login')).rejects.toMatchObject({
      message: 'API request failed with an empty response.',
      status: 502,
      errorCode: null
    });
  });

  it('throws a friendly API error for non-JSON responses', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      mockApiResponse({
        ok: false,
        status: 500,
        body: '<html>Server error</html>'
      })
    );
    vi.stubGlobal('fetch', fetchMock);

    await expect(requestApi('/api/auth/login')).rejects.toMatchObject({
      message: 'API returned an unexpected response format.',
      status: 500,
      errorCode: null
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

  it('adds a bearer token when one is held in memory', async () => {
    setAuthToken('test-token');
    const fetchMock = vi.fn().mockResolvedValue(
      mockApiResponse({
        ok: true,
        status: 200,
        body: {
          success: true,
          message: null,
          data: { id: 'user-1', email: 'user@example.com', roles: ['User'] },
          errorCode: null
        }
      })
    );
    vi.stubGlobal('fetch', fetchMock);

    await requestApi('/api/auth/me');

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(init.headers).toBeInstanceOf(Headers);
    expect((init.headers as Headers).get('Authorization')).toBe('Bearer test-token');
  });

  it('uses one refresh request for concurrent 401 responses and retries each request once', async () => {
    setAuthToken('expired-token');
    const fetchMock = vi.fn().mockImplementation(async (path: string, init: RequestInit) => {
      if (path === '/api/auth/refresh') {
        return mockApiResponse({
          ok: true,
          status: 200,
          body: {
            success: true,
            message: null,
            data: {
              accessToken: 'renewed-token',
              expiresAt: '2026-07-16T00:00:00Z',
              user: { id: 'user-1', email: 'user@example.com', roles: ['User'] }
            },
            errorCode: null
          }
        });
      }

      const authorization = (init.headers as Headers).get('Authorization');
      return authorization === 'Bearer renewed-token'
        ? mockApiResponse({
            ok: true,
            status: 200,
            body: { success: true, message: null, data: [], errorCode: null }
          })
        : mockApiResponse({
            ok: false,
            status: 401,
            body: { success: false, message: 'Expired.', data: null, errorCode: 'unauthorized' }
          });
    });
    vi.stubGlobal('fetch', fetchMock);

    await Promise.all([
      requestApi('/api/todos'),
      requestApi('/api/todos'),
      requestApi('/api/todos')
    ]);

    expect(fetchMock.mock.calls.filter(([path]) => path === '/api/auth/refresh')).toHaveLength(1);
    expect(fetchMock.mock.calls.filter(([path]) => path === '/api/todos')).toHaveLength(6);
  });

  it('does not recurse when refresh fails', async () => {
    setAuthToken('expired-token');
    const expiredListener = vi.fn();
    const stopListening = onSessionExpired(expiredListener);
    const fetchMock = vi.fn().mockImplementation(async (path: string) => mockApiResponse({
      ok: false,
      status: 401,
      body: {
        success: false,
        message: path === '/api/auth/refresh' ? 'Refresh invalid.' : 'Access expired.',
        data: null,
        errorCode: 'unauthorized'
      }
    }));
    vi.stubGlobal('fetch', fetchMock);

    await expect(requestApi('/api/todos')).rejects.toMatchObject({ status: 401 });

    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(fetchMock.mock.calls.filter(([path]) => path === '/api/auth/refresh')).toHaveLength(1);
    expect(expiredListener).toHaveBeenCalledOnce();
    stopListening();
  });

  it('retries only once when the renewed access token is also rejected', async () => {
    setAuthToken('expired-token');
    const fetchMock = vi.fn().mockImplementation(async (path: string) => {
      if (path === '/api/auth/refresh') {
        return mockApiResponse({
          ok: true,
          status: 200,
          body: {
            success: true,
            message: null,
            data: {
              accessToken: 'renewed-token',
              expiresAt: '2026-07-16T00:00:00Z',
              user: { id: 'user-1', email: 'user@example.com', roles: ['User'] }
            },
            errorCode: null
          }
        });
      }
      return mockApiResponse({
        ok: false,
        status: 401,
        body: { success: false, message: 'Rejected.', data: null, errorCode: 'unauthorized' }
      });
    });
    vi.stubGlobal('fetch', fetchMock);

    await expect(requestApi('/api/todos')).rejects.toMatchObject({ status: 401 });

    expect(fetchMock).toHaveBeenCalledTimes(3);
    expect(fetchMock.mock.calls.filter(([path]) => path === '/api/auth/refresh')).toHaveLength(1);
  });
});
