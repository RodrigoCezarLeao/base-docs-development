import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { projectKeys } from './keys'
import type { ApiResponse } from '@/types'
import type { ProjectDto } from './types'

export function useProjects() {
  return useQuery({
    queryKey: projectKeys.lists(),
    queryFn: () => api.get<ApiResponse<ProjectDto[]>>('/api/v1/projects'),
    select: (response) => response.data ?? [],
  })
}

export function useProject(id: number) {
  return useQuery({
    queryKey: projectKeys.detail(id),
    queryFn: () => api.get<ApiResponse<ProjectDto>>(`/api/v1/projects/${id}`),
    select: (response) => response.data,
    enabled: !!id,
  })
}
