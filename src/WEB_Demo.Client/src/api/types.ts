export type ApiResponse<T> = {
  success: boolean;
  message: string | null;
  data: T | null;
  errorCode: string | null;
};

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly errorCode: string | null
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export type TodoItem = {
  id: number;
  title: string;
  isDone: boolean;
};

export type CreateTodoRequest = {
  title: string;
};

export type AuthUser = {
  id: string;
  email: string;
  roles: string[];
};

export type AuthResponse = {
  accessToken: string;
  expiresAt: string;
  user: AuthUser;
};

export type RegisterRequest = {
  email: string;
  password: string;
};

export type LoginRequest = {
  email: string;
  password: string;
};

export type AdminUser = {
  id: string;
  email: string;
  isEnabled: boolean;
  roles: string[];
};

export type AdminRole = {
  id: string;
  name: string;
  isSystem: boolean;
  userCount: number;
};

export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
};
