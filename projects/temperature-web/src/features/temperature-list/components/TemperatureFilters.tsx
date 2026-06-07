import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/Button'

interface TemperatureFiltersProps {
  locationFilter: string
  onLocationChange: (value: string) => void
  onClear: () => void
}

export function TemperatureFilters({ locationFilter, onLocationChange, onClear }: TemperatureFiltersProps) {
  const { t } = useTranslation()

  return (
    <div style={{ display: 'flex', gap: '8px', marginBottom: '16px' }}>
      <input
        type="text"
        placeholder={t('temperature.filterByLocation')}
        value={locationFilter}
        onChange={(e) => onLocationChange(e.target.value)}
        style={{ padding: '8px 12px', border: '1px solid #d1d5db', borderRadius: '6px', fontSize: '14px', flex: 1 }}
      />
      {locationFilter && (
        <Button variant="secondary" onClick={onClear}>
          {t('common.cancel')}
        </Button>
      )}
    </div>
  )
}
