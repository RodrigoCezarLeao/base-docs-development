import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api'
import type { ApiResponse, PagedResponse } from '@/types'
import { temperatureKeys } from './keys'
import type { TemperatureReading } from './types'

export function useTemperatures(page = 1, pageSize = 10) {
  return useQuery({
    queryKey: temperatureKeys.list(page, pageSize),
    queryFn: () =>
      api.get<ApiResponse<PagedResponse<TemperatureReading>>>('/api/v1/temperaturereadings', {
        params: { page, pageSize },
      }) as Promise<ApiResponse<PagedResponse<TemperatureReading>>>,
  })
}

export function useTemperature(id: number) {
  return useQuery({
    queryKey: temperatureKeys.detail(id),
    queryFn: () =>
      api.get<ApiResponse<TemperatureReading>>(`/api/v1/temperaturereadings/${id}`) as Promise<
        ApiResponse<TemperatureReading>
      >,
    enabled: !!id,
  })
}
