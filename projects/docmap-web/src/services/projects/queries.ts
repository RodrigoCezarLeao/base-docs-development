import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { cacheTiers } from '@/lib/cache'
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
  // A project's name/description rarely change — cache it on the stable tier
  // (matches the backend cache-aside on GET /projects/{id}).
  return useQuery({
    ...cacheTiers.stable,
    queryKey: projectKeys.detail(id),
    queryFn: () => api.get<ApiResponse<ProjectDto>>(`/api/v1/projects/${id}`),
    select: (response) => response.data,
    enabled: !!id,
  })
}
