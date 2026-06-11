import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api'
import type { ApiResponse } from '@/types'
import { accessKeys } from './keys'
import type { AccessEventDto, AccessFilters, PagedResponse } from './types'

export function useAccessEvents(filters: AccessFilters) {
  return useQuery({
    queryKey: accessKeys.list(filters),
    queryFn: () =>
      api.get<ApiResponse<PagedResponse<AccessEventDto>>>('/api/v1/admin/access', { params: { ...filters } }),
    select: (response) => response.data,
  })
}
