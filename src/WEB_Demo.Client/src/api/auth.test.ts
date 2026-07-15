import { afterEach, describe, expect, it, vi } from 'vitest';
import { login, logout, register } from './auth';
import { clearAuthToken, getAuthToken } from './authToken';

function mockApiResponse<T>(body: T) {
  return {
    ok: true,
    status: 200,
    text: vi.fn().mockResolvedValue(JSON.stringify(body))
  } as unknown as Response;
}

describe('auth API', () => {
  afterEach(() => {
    window.localStorage.clear();
    clearAuthToken();
    vi.unstubAllGlobals();
  });

  it('logs in and keeps the access token only in memory', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(
      mockApiResponse({
        success: true,
        message: null,
        data: {
          accessToken: 'login-token',
          expiresAt: '2026-07-06T00:00:00Z',
          user: { id: 'user-1', email: 'user@example.com', roles: ['User'] }
        },
        errorCode: null
      })
    ));

    const response = await login({ email: 'user@example.com', password: '123456' });

    expect(response?.accessToken).toBe('login-token');
    expect(getAuthToken()).toBe('login-token');
    expect(window.localStorage.getItem('web_demo.auth_token')).toBeNull();
  });

  it('registers and keeps the access token only in memory', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(
      mockApiResponse({
        success: true,
        message: null,
        data: {
          accessToken: 'register-token',
          expiresAt: '2026-07-06T00:00:00Z',
          user: { id: 'user-2', email: 'new@example.com', roles: ['User'] }
        },
        errorCode: null
      })
    ));

    await register({ email: 'new@example.com', password: '123456' });

    expect(getAuthToken()).toBe('register-token');
    expect(window.localStorage.getItem('web_demo.auth_token')).toBeNull();
  });

  it('revokes the backend session and clears the access token on logout', async () => {
    window.localStorage.setItem('web_demo.auth_token', 'old-token');
    const fetchMock = vi.fn().mockResolvedValue(mockApiResponse({
      success: true,
      message: 'Logged out.',
      data: null,
      errorCode: null
    }));
    vi.stubGlobal('fetch', fetchMock);

    await logout();

    expect(window.localStorage.getItem('web_demo.auth_token')).toBeNull();
    expect(fetchMock).toHaveBeenCalledWith('/api/auth/logout', expect.objectContaining({ method: 'POST' }));
  });

  it('clears the access token when backend logout is unauthorized', async () => {
    window.localStorage.setItem('web_demo.auth_token', 'expired-token');
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: false,
      status: 401,
      text: vi.fn().mockResolvedValue(JSON.stringify({
        success: false,
        message: 'Authentication is required.',
        data: null,
        errorCode: 'Unauthorized'
      }))
    } as unknown as Response));

    await logout();

    expect(window.localStorage.getItem('web_demo.auth_token')).toBeNull();
  });

  it('clears the access token when backend logout has a network failure', async () => {
    window.localStorage.setItem('web_demo.auth_token', 'old-token');
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('offline')));

    await logout();

    expect(window.localStorage.getItem('web_demo.auth_token')).toBeNull();
  });
});
