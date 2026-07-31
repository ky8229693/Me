const legacyAuthTokenKey = 'web_demo.auth_token';
let authToken: string | null = null;

if (typeof window !== 'undefined') {
  window.localStorage.removeItem(legacyAuthTokenKey);
}

export function getAuthToken(): string | null {
  return authToken;
}

export function setAuthToken(token: string): void {
  authToken = token;
}

export function clearAuthToken(): void {
  authToken = null;
  if (typeof window !== 'undefined') {
    window.localStorage.removeItem(legacyAuthTokenKey);
  }
}
