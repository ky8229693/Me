import { ApiError, type ApiResponse } from './types';
import { getAuthToken } from './authToken';

type ApiRequestOptions = Omit<RequestInit, 'body'> & {
  body?: unknown;
};

export async function requestApi<T>(path: string, options: ApiRequestOptions = {}): Promise<T | null> {
  const { body, headers, ...init } = options;
  const requestHeaders = new Headers(headers);
  const hasBody = body !== undefined;

  if (hasBody && !requestHeaders.has('Content-Type')) {
    requestHeaders.set('Content-Type', 'application/json');
  }

  const authToken = getAuthToken();
  if (authToken && !requestHeaders.has('Authorization')) {
    requestHeaders.set('Authorization', `Bearer ${authToken}`);
  }

  const response = await fetch(path, {
    ...init,
    headers: requestHeaders,
    body: hasBody ? JSON.stringify(body) : undefined
  });

  const payload = (await response.json()) as ApiResponse<T>;

  if (!response.ok || !payload.success) {
    throw new ApiError(
      payload.message ?? 'API request failed.',
      response.status,
      payload.errorCode
    );
  }

  return payload.data;
}
