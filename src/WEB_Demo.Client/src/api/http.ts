import { ApiError, type ApiResponse } from './types';
import { getAuthToken } from './authToken';

type ApiRequestOptions = Omit<RequestInit, 'body'> & {
  body?: unknown;
  skipAuthRefresh?: boolean;
};

let refreshPromise: Promise<boolean> | null = null;

const nonRefreshableAuthPaths = new Set([
  '/api/auth/login',
  '/api/auth/register',
  '/api/auth/refresh',
  '/api/auth/logout'
]);

export async function requestApi<T>(path: string, options: ApiRequestOptions = {}): Promise<T | null> {
  return requestApiInternal<T>(path, options, false);
}

async function requestApiInternal<T>(
  path: string,
  options: ApiRequestOptions,
  retried: boolean
): Promise<T | null> {
  const { body, headers, skipAuthRefresh = false, ...init } = options;
  const requestHeaders = new Headers(headers);
  const hasBody = body !== undefined;

  if (hasBody && !requestHeaders.has('Content-Type')) {
    requestHeaders.set('Content-Type', 'application/json');
  }

  const authToken = getAuthToken();
  if (authToken && !requestHeaders.has('Authorization')) {
    requestHeaders.set('Authorization', `Bearer ${authToken}`);
  }

  let response: Response;

  try {
    response = await fetch(path, {
      ...init,
      credentials: init.credentials ?? 'include',
      headers: requestHeaders,
      body: hasBody ? JSON.stringify(body) : undefined
    });
  } catch {
    throw new ApiError('Unable to connect to the API. Please make sure the backend is running.', 0, null);
  }

  if (
    response.status === 401 &&
    !retried &&
    !skipAuthRefresh &&
    !nonRefreshableAuthPaths.has(path)
  ) {
    const refreshed = await refreshAccessToken();
    if (refreshed) {
      return requestApiInternal<T>(path, options, true);
    }
  }

  const responseText = await response.text();
  let payload: ApiResponse<T> | null = null;

  if (responseText) {
    try {
      payload = JSON.parse(responseText) as ApiResponse<T>;
    } catch {
      throw new ApiError('API returned an unexpected response format.', response.status, null);
    }
  }

  if (!payload) {
    if (response.ok) {
      return null;
    }

    throw new ApiError('API request failed with an empty response.', response.status, null);
  }

  if (!response.ok || !payload.success) {
    throw new ApiError(
      payload.message ?? 'API request failed.',
      response.status,
      payload.errorCode
    );
  }

  return payload.data;
}

async function refreshAccessToken(): Promise<boolean> {
  if (!refreshPromise) {
    refreshPromise = import('./auth')
      .then(({ refreshSession }) => refreshSession())
      .then(response => Boolean(response?.accessToken))
      .catch(() => false)
      .finally(() => {
        refreshPromise = null;
      });
  }

  return refreshPromise;
}
