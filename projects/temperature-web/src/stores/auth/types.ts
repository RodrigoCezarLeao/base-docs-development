import type { UserDto } from '@/services/auth/types'

export interface AuthState {
  user: UserDto | null
  token: string | null
}

export interface AuthActions {
  setAuth: (token: string, user: UserDto) => void
  logout: () => void
}

export type AuthStore = AuthState & AuthActions
