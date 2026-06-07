import { useTranslation } from 'react-i18next'
import { Pagination } from '@/components/ui/Pagination'
import { Spinner } from '@/components/ui/Spinner'
import type { TemperatureReading } from '@/services/temperatures/types'
import { TemperatureCard } from './TemperatureCard'

interface TemperatureListProps {
  readings: TemperatureReading[]
  isLoading: boolean
  isError: boolean
  page: number
  totalPages: number
  onPageChange: (page: number) => void
  locationFilter: string
}

export function TemperatureList({
  readings,
  isLoading,
  isError,
  page,
  totalPages,
  onPageChange,
  locationFilter,
}: TemperatureListProps) {
  const { t } = useTranslation()

  if (isLoading) return <div className="flex justify-center py-12"><Spinner size="lg" /></div>
  if (isError) return <p className="text-red-600">{t('common.error')}</p>

  const filtered = locationFilter
    ? readings.filter((r) => r.location.toLowerCase().includes(locationFilter.toLowerCase()))
    : readings

  if (filtered.length === 0) return <p className="text-gray-400">{t('common.noData')}</p>

  return (
    <div>
      <div className="grid gap-3 grid-cols-[repeat(auto-fill,minmax(280px,1fr))]">
        {filtered.map((reading) => (
          <TemperatureCard key={reading.id} reading={reading} />
        ))}
      </div>
      <Pagination page={page} totalPages={totalPages} onPageChange={onPageChange} />
    </div>
  )
}
