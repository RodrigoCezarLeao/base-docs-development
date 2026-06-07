import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/Button'
import type { CreateTemperatureReadingDto } from '@/services/temperatures/types'

interface AddTemperatureFormProps {
  onSubmit: (dto: CreateTemperatureReadingDto) => void
  isLoading: boolean
}

export function AddTemperatureForm({ onSubmit, isLoading }: AddTemperatureFormProps) {
  const { t } = useTranslation()
  const [location, setLocation] = useState('')
  const [valueCelsius, setValueCelsius] = useState('')

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!location || !valueCelsius) return

    onSubmit({ location, valueCelsius: parseFloat(valueCelsius), recordedAt: new Date().toISOString() })
    setLocation('')
    setValueCelsius('')
  }

  return (
    <form onSubmit={handleSubmit} style={{ display: 'flex', gap: '8px', flexWrap: 'wrap', marginBottom: '24px' }}>
      <input
        placeholder={t('temperature.location')}
        value={location}
        onChange={(e) => setLocation(e.target.value)}
        required
        style={{ padding: '8px 12px', border: '1px solid #d1d5db', borderRadius: '6px', fontSize: '14px' }}
      />
      <input
        type="number"
        step="0.1"
        placeholder={`${t('temperature.value')} (°C)`}
        value={valueCelsius}
        onChange={(e) => setValueCelsius(e.target.value)}
        required
        style={{ padding: '8px 12px', border: '1px solid #d1d5db', borderRadius: '6px', fontSize: '14px', width: '140px' }}
      />
      <Button type="submit" loading={isLoading}>
        {t('common.add')}
      </Button>
    </form>
  )
}
