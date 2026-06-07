import { useTranslation } from 'react-i18next'
import { formatCelsius, formatDateTime } from '@/helpers/format'
import type { TemperatureReading } from '@/services/temperatures/types'

interface TemperatureCardProps {
  reading: TemperatureReading
}

export function TemperatureCard({ reading }: TemperatureCardProps) {
  const { t } = useTranslation()

  return (
    <div style={{ border: '1px solid #e5e7eb', borderRadius: '8px', padding: '16px', backgroundColor: '#fff' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
        <span style={{ fontWeight: 600, color: '#374151' }}>{reading.location}</span>
        <span style={{ fontSize: '24px', fontWeight: 700, color: '#2563eb' }}>
          {formatCelsius(reading.valueCelsius)}
        </span>
      </div>
      <div style={{ marginTop: '8px', fontSize: '13px', color: '#9ca3af' }}>
        {t('temperature.recordedAt')}: {formatDateTime(reading.recordedAt)}
      </div>
    </div>
  )
}
