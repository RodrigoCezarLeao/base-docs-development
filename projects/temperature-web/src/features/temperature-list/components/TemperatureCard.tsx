import { useTranslation } from 'react-i18next'
import { formatCelsius, formatDateTime } from '@/helpers/format'
import type { TemperatureReading } from '@/services/temperatures/types'

interface TemperatureCardProps {
  reading: TemperatureReading
}

export function TemperatureCard({ reading }: TemperatureCardProps) {
  const { t } = useTranslation()

  return (
    <div className="border border-gray-200 dark:border-gray-700 rounded-lg p-4 bg-white dark:bg-gray-800 shadow-sm">
      <div className="flex justify-between items-start">
        <span className="font-semibold text-gray-700 dark:text-gray-200">{reading.location}</span>
        <span className="text-2xl font-bold text-blue-600 dark:text-blue-400">
          {formatCelsius(reading.valueCelsius)}
        </span>
      </div>
      <p className="mt-2 text-xs text-gray-400 dark:text-gray-500">
        {t('temperature.recordedAt')}: {formatDateTime(reading.recordedAt)}
      </p>
    </div>
  )
}
