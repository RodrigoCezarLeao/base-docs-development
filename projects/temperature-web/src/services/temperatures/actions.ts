import { useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api'
import type { ApiResponse } from '@/types'
import { temperatureKeys } from './keys'
import type { CreateTemperatureReadingDto, TemperatureReading } from './types'

export function useCreateTemperature() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (dto: CreateTemperatureReadingDto) =>
      api.post<ApiResponse<TemperatureReading>>('/api/v1/temperaturereadings', dto) as Promise<
        ApiResponse<TemperatureReading>
      >,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: temperatureKeys.lists() })
    },
  })
}

export function useDeleteTemperature() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: number) => api.delete(`/api/v1/temperaturereadings/${id}`),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: temperatureKeys.lists() })
      queryClient.removeQueries({ queryKey: temperatureKeys.detail(id) })
    },
  })
}
