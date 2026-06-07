import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { documentKeys } from './keys'
import type { ApiResponse } from '@/types'
import type { DocumentDto } from './types'

export function useDocuments(projectId: number) {
  return useQuery({
    queryKey: documentKeys.lists(projectId),
    queryFn: () =>
      api.get<ApiResponse<DocumentDto[]>>(`/api/v1/projects/${projectId}/documents`),
    select: (response) => response.data ?? [],
    enabled: !!projectId,
  })
}

export function useDocument(projectId: number, id: number) {
  return useQuery({
    queryKey: documentKeys.detail(projectId, id),
    queryFn: () =>
      api.get<ApiResponse<DocumentDto>>(`/api/v1/projects/${projectId}/documents/${id}`),
    select: (response) => response.data,
    enabled: !!id,
  })
}
