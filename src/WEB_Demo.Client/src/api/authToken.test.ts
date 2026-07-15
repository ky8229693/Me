import { beforeEach, describe, expect, it, vi } from 'vitest';

describe('memory access token storage', () => {
  beforeEach(() => {
    vi.resetModules();
    window.localStorage.clear();
  });

  it('removes the legacy persistent token when the module starts', async () => {
    window.localStorage.setItem('web_demo.auth_token', 'legacy-token');

    const storage = await import('./authToken');

    expect(window.localStorage.getItem('web_demo.auth_token')).toBeNull();
    expect(storage.getAuthToken()).toBeNull();
  });

  it('keeps new access tokens in module memory only', async () => {
    const storage = await import('./authToken');

    storage.setAuthToken('memory-token');

    expect(storage.getAuthToken()).toBe('memory-token');
    expect(window.localStorage.getItem('web_demo.auth_token')).toBeNull();
  });
});
