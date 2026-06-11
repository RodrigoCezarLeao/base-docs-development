import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { cacheTiers } from '@/lib/cache'
import type { ApiResponse, PagedResponse } from '@/types'
import { temperatureKeys } from './keys'
import type { TemperatureReading } from './types'

export function useTemperatures(page = 1, pageSize = 10) {
  return useQuery({
    queryKey: temperatureKeys.list(page, pageSize),
    queryFn: () =>
      api.get<ApiResponse<PagedResponse<TemperatureReading>>>('/api/v1/temperaturereadings', {
        params: { page, pageSize },
      }),
  })
}

export function useTemperature(id: number) {
  // A single reading rarely changes once recorded — cache it on the stable tier
  // (matches the backend cache-aside on GET /temperaturereadings/{id}).
  return useQuery({
    ...cacheTiers.stable,
    queryKey: temperatureKeys.detail(id),
    queryFn: () =>
      api.get<ApiResponse<TemperatureReading>>(`/api/v1/temperaturereadings/${id}`),
    enabled: !!id,
  })
}
