import { useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { projectKeys } from './keys'
import type { ApiResponse } from '@/types'
import type { CreateProjectDto, ProjectDto } from './types'

export function useCreateProject() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (dto: CreateProjectDto) =>
      api.post<ApiResponse<ProjectDto>>('/api/v1/projects', dto) as Promise<ApiResponse<ProjectDto>>,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: projectKeys.lists() })
    },
  })
}

export function useDeleteProject() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: number) =>
      api.delete(`/api/v1/projects/${id}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: projectKeys.lists() })
    },
  })
}
