import { useState } from 'react'
import { useTemperatures } from '@/services/temperatures/queries'
import { useCreateTemperature } from '@/services/temperatures/actions'
import { useTemperatureFilters } from '@/stores/temperature/hooks'
import type { CreateTemperatureReadingDto } from '@/services/temperatures/types'

export function useTemperatureList() {
  const [page, setPage] = useState(1)
  const pageSize = 10

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
