import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api'
import type { ApiResponse } from '@/types'
import type { MetricsSnapshot } from './types'

/** Polls the admin metrics snapshot every ~2s for a near-real-time dashboard. */
export function useMetrics() {
  return useQuery({
    queryKey: ['metrics'],
    queryFn: () => api.get<ApiResponse<MetricsSnapshot>>('/api/v1/admin/metrics'),
    select: (response) => response.data,
    refetchInterval: 2000,
    staleTime: 0,
  })
}
