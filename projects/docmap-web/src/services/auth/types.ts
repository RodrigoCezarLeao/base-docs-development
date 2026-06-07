export interface UserDto {
  id: number
  email: string
  name: string
}

export interface AuthResponse {
  token: string
  user: UserDto
}

export interface LoginDto {
  email: string
  password: string
}

export interface RegisterDto {
  name: string
  email: string
  password: string
}
