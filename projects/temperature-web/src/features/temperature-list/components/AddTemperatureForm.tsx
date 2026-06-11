import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/Button'
import type { CreateTemperatureReadingDto } from '@/services/temperatures/types'

interface AddTemperatureFormProps {
  onSubmit: (dto: CreateTemperatureReadingDto) => void
  isLoading: boolean
}

const inputClass = 'px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md text-sm bg-white text-gray-900 dark:bg-gray-800 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500'

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
    <form onSubmit={handleSubmit} className="flex flex-wrap gap-2 mb-6">
      <input
        placeholder={t('temperature.location')}
        value={location}
        onChange={(e) => setLocation(e.target.value)}
        required
        className={inputClass}
      />
      <input
        type="number"
        step="0.1"
        placeholder={`${t('temperature.value')} (°C)`}
        value={valueCelsius}
        onChange={(e) => setValueCelsius(e.target.value)}
        required
        className={`${inputClass} w-36`}
      />
      <Button type="submit" loading={isLoading}>
        {t('common.add')}
      </Button>
    </form>
  )
}
