import { usePagination } from '@/hooks/usePagination'
import { useCreateTemperature } from '@/services/temperatures/actions'
import { useTemperatures } from '@/services/temperatures/queries'
import type { CreateTemperatureReadingDto } from '@/services/temperatures/types'
import { useTemperatureFilters } from '@/stores/temperature/hooks'

export function useTemperatureList() {
  const { page, pageSize, setPage } = usePagination()
  const { locationFilter, setLocationFilter, clearFilters } = useTemperatureFilters()
  const { data, isLoading, isError } = useTemperatures(page, pageSize)
  const createMutation = useCreateTemperature()

  const readings = data?.data?.items ?? []
  const totalPages = data?.data?.totalPages ?? 1

  function handleCreate(dto: CreateTemperatureReadingDto) {
    createMutation.mutate(dto)
  }

  return {
    readings,
    isLoading,
    isError,
    page,
    totalPages,
    setPage,
    locationFilter,
    setLocationFilter,
    clearFilters,
    handleCreate,
    isCreating: createMutation.isPending,
  }
}
