export interface CreateUserRequest {
  email: string;
  displayName: string;
}

export interface UserDto {
  id: string;
  displayName: string;
  email: string;
}
