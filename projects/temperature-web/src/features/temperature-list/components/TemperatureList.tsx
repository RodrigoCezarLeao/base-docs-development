import { useTranslation } from 'react-i18next'
import { Spinner } from '@/components/ui/Spinner'
import { Button } from '@/components/ui/Button'
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

export function TemperatureList({ readings, isLoading, isError, page, totalPages, onPageChange, locationFilter }: TemperatureListProps) {
  const { t } = useTranslation()

  if (isLoading) return <Spinner />
  if (isError) return <p style={{ color: '#dc2626' }}>{t('common.error')}</p>

  const filtered = locationFilter
    ? readings.filter((r) => r.location.toLowerCase().includes(locationFilter.toLowerCase()))
    : readings

  if (filtered.length === 0) return <p style={{ color: '#9ca3af' }}>{t('common.noData')}</p>

  return (
    <div>
      <div style={{ display: 'grid', gap: '12px', gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))' }}>
        {filtered.map((reading) => (
          <TemperatureCard key={reading.id} reading={reading} />
        ))}
      </div>
      <div style={{ display: 'flex', gap: '8px', justifyContent: 'center', marginTop: '24px' }}>
        <Button variant="secondary" disabled={page <= 1} onClick={() => onPageChange(page - 1)}>←</Button>
        <span style={{ padding: '8px 16px', fontSize: '14px', color: '#374151' }}>{page} / {totalPages}</span>
        <Button variant="secondary" disabled={page >= totalPages} onClick={() => onPageChange(page + 1)}>→</Button>
      </div>
    </div>
  )
}
