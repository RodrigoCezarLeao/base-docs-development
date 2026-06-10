import { useMutation } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { useAuthActions } from '@/stores/auth/hooks'
import type { ApiResponse } from '@/types'
import type { AuthResponse, LoginDto, RegisterDto } from './types'

export function useLogin() {
  const { setAuth } = useAuthActions()

  return useMutation({
    mutationFn: (dto: LoginDto) =>
      api.post<ApiResponse<AuthResponse>>('/api/v1/auth/login', dto),
    onSuccess: (response) => {
      if (response.data) setAuth(response.data.token, response.data.user)
    },
  })
}

export function useRegister() {
  const { setAuth } = useAuthActions()

  return useMutation({
    mutationFn: (dto: RegisterDto) =>
      api.post<ApiResponse<AuthResponse>>('/api/v1/auth/register', dto),
    onSuccess: (response) => {
      if (response.data) setAuth(response.data.token, response.data.user)
    },
  })
}
