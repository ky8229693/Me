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
