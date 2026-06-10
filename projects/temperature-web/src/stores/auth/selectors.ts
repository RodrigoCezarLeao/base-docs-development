import type { AuthState } from './types'

export const selectUser = (state: AuthState) => state.user
export const selectToken = (state: AuthState) => state.token
export const selectIsAuthenticated = (state: AuthState) => state.user !== null && state.token !== null
export const selectIsAdmin = (state: AuthState) => state.user?.isAdmin === true
