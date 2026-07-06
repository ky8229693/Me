const authTokenKey = 'web_demo.auth_token';

export function getAuthToken(): string | null {
  return window.localStorage.getItem(authTokenKey);
}

export function setAuthToken(token: string): void {
  window.localStorage.setItem(authTokenKey, token);
}

export function clearAuthToken(): void {
  window.localStorage.removeItem(authTokenKey);
}
