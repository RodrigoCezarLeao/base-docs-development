import { useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { connectionKeys } from './keys'
import { documentKeys } from '@/services/documents/keys'
import type { ApiResponse } from '@/types'
import type { ConnectionDto, CreateConnectionDto } from './types'

export function useCreateConnection(projectId: number) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (dto: CreateConnectionDto) =>
      api.post<ApiResponse<ConnectionDto>>(`/api/v1/projects/${projectId}/connections`, dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: connectionKeys.lists(projectId) })
      // Invalidate all document queries (list + detail) — connections modify document content
      queryClient.invalidateQueries({ queryKey: documentKeys.all(projectId) })
    },
  })
}

export function useDeleteConnection(projectId: number) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: number) => api.delete(`/api/v1/projects/${projectId}/connections/${id}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: connectionKeys.lists(projectId) })
      // Invalidate all document queries (list + detail) — deletion removes the reference from markdown
      queryClient.invalidateQueries({ queryKey: documentKeys.all(projectId) })
    },
  })
}
