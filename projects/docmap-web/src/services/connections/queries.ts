import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { connectionKeys } from './keys'
import type { ApiResponse } from '@/types'
import type { ConnectionDto } from './types'

export function useConnections(projectId: number) {
  return useQuery({
    queryKey: connectionKeys.lists(projectId),
    queryFn: () =>
      api.get<ApiResponse<ConnectionDto[]>>(`/api/v1/projects/${projectId}/connections`),
    select: (response) => response.data ?? [],
    enabled: !!projectId,
  })
}
