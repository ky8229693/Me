import { afterEach, describe, expect, it, vi } from 'vitest';
import { login, logout, register } from './auth';

function mockApiResponse<T>(body: T) {
  return {
    ok: true,
    status: 200,
    json: vi.fn().mockResolvedValue(body)
  } as unknown as Response;
}

describe('auth API', () => {
  afterEach(() => {
    window.localStorage.clear();
    vi.unstubAllGlobals();
  });

  it('logs in and stores the access token', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(
      mockApiResponse({
        success: true,
        message: null,
        data: {
          accessToken: 'login-token',
          expiresAt: '2026-07-06T00:00:00Z',
          user: { id: 'user-1', email: 'user@example.com' }
        },
        errorCode: null
      })
    ));

    const response = await login({ email: 'user@example.com', password: '123456' });

    expect(response?.accessToken).toBe('login-token');
    expect(window.localStorage.getItem('web_demo.auth_token')).toBe('login-token');
  });

  it('registers and stores the access token', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(
      mockApiResponse({
        success: true,
        message: null,
        data: {
          accessToken: 'register-token',
          expiresAt: '2026-07-06T00:00:00Z',
          user: { id: 'user-2', email: 'new@example.com' }
        },
        errorCode: null
      })
    ));

    await register({ email: 'new@example.com', password: '123456' });

    expect(window.localStorage.getItem('web_demo.auth_token')).toBe('register-token');
  });

  it('clears the access token on logout', () => {
    window.localStorage.setItem('web_demo.auth_token', 'old-token');

    logout();

    expect(window.localStorage.getItem('web_demo.auth_token')).toBeNull();
  });
});
