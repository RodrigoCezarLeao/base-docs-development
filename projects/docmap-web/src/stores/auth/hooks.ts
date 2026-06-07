import { useShallow } from 'zustand/react/shallow'
import { useAuthStore } from './store'
import { selectUser, selectIsAuthenticated } from './selectors'

export function useAuth() {
  return useAuthStore(
    useShallow((state) => ({
      user: selectUser(state),
      isAuthenticated: selectIsAuthenticated(state),
      logout: state.logout,
    })),
  )
}

export function useAuthActions() {
  return useAuthStore(
    useShallow((state) => ({
      setAuth: state.setAuth,
      logout: state.logout,
    })),
  )
}
