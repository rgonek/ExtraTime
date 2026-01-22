export interface User {
  id: string;
  email: string;
  username: string;
}

export interface ApiError {
  message: string;
  statusCode?: number;
}
