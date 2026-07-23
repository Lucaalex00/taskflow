export interface CreateUserRequest {
  email: string;
  displayName: string;
  password: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResult {
  userId: string;
  displayName: string;
  token: string;
}

export interface UserDto {
  id: string;
  displayName: string;
  email: string;
}
