import { clearAuthToken, setAuthToken } from './authToken';
import { requestApi } from './http';
import type { AuthResponse, AuthUser, LoginRequest, RegisterRequest } from './types';

export async function register(request: RegisterRequest): Promise<AuthResponse | null> {
  const response = await requestApi<AuthResponse>('/api/auth/register', {
    method: 'POST',
    body: request
  });

  if (response?.accessToken) {
    setAuthToken(response.accessToken);
  }

  return response;
}

export async function login(request: LoginRequest): Promise<AuthResponse | null> {
  const response = await requestApi<AuthResponse>('/api/auth/login', {
    method: 'POST',
    body: request
  });

  if (response?.accessToken) {
    setAuthToken(response.accessToken);
  }

  return response;
}

export async function logout(): Promise<void> {
  try {
    await requestApi<object>('/api/auth/logout', { method: 'POST' });
  } catch {
    // Local logout must still complete when the session is already invalid or the API is unavailable.
  } finally {
    clearAuthToken();
  }
}

export function getCurrentUser(): Promise<AuthUser | null> {
  return requestApi<AuthUser>('/api/auth/me');
}
