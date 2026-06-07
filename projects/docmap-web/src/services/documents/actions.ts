import { useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { documentKeys } from './keys'
import type { ApiResponse } from '@/types'
import type { CreateDocumentDto, DocumentDto, UpdateDocumentDto, UpdatePositionDto } from './types'

export function useCreateDocument(projectId: number) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (dto: CreateDocumentDto) =>
      api.post<ApiResponse<DocumentDto>>(
        `/api/v1/projects/${projectId}/documents`,
        dto,
      ) as Promise<ApiResponse<DocumentDto>>,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: documentKeys.lists(projectId) })
    },
  })
}

export function useUpdateDocument(projectId: number) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, dto }: { id: number; dto: UpdateDocumentDto }) =>
      api.put<ApiResponse<DocumentDto>>(
        `/api/v1/projects/${projectId}/documents/${id}`,
        dto,
      ) as Promise<ApiResponse<DocumentDto>>,
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: documentKeys.lists(projectId) })
      queryClient.invalidateQueries({
        queryKey: documentKeys.detail(projectId, variables.id),
      })
    },
  })
}

export function useUpdatePosition(projectId: number) {
  return useMutation({
    mutationFn: ({ id, dto }: { id: number; dto: UpdatePositionDto }) =>
      api.patch(
        `/api/v1/projects/${projectId}/documents/${id}/position`,
        dto,
      ),
  })
}

export function useDeleteDocument(projectId: number) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: number) =>
      api.delete(`/api/v1/projects/${projectId}/documents/${id}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: documentKeys.lists(projectId) })
    },
  })
}
