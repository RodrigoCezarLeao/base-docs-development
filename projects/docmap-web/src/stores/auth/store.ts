import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import { tokenStorage } from '@/lib/auth'
import type { AuthStore } from './types'

export const useAuthStore = create<AuthStore>()(
  persist(
    (set) => ({
      user: null,
      token: null,

      setAuth: (token, user) => {
        tokenStorage.set(token)
        set({ token, user })
      },

      logout: () => {
        tokenStorage.clear()
        set({ token: null, user: null })
      },
    }),
    { name: 'docmap-auth' },
  ),
)
