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
    <div className="flex gap-2 mb-4">
      <input
        type="text"
        placeholder={t('temperature.filterByLocation')}
        value={locationFilter}
        onChange={(e) => onLocationChange(e.target.value)}
        className="flex-1 px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md text-sm bg-white text-gray-900 dark:bg-gray-800 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
      />
      {locationFilter && (
        <Button variant="secondary" onClick={onClear}>
          {t('common.cancel')}
        </Button>
      )}
    </div>
  )
}
