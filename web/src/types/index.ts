// User
export interface User {
  id: string;
  email: string;
  username: string;
  role: 'User' | 'Admin';
}

// Auth API Requests
export interface RegisterRequest {
  email: string;
  username: string;
  password: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

// Auth API Responses
export interface AuthResponse {
  user: User;
  accessToken: string;
  refreshToken: string;
}

export interface CurrentUserResponse {
  id: string;
  email: string;
  username: string;
  role: string;
}

// API Error
export interface ApiError {
  message: string;
  statusCode?: number;
  errors?: Record<string, string[]>;
}
