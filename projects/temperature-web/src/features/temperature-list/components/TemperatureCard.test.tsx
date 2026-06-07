import { render, screen } from '@testing-library/react'
import { describe, it, expect, vi } from 'vitest'
import { TemperatureCard } from './TemperatureCard'
import type { TemperatureReading } from '@/services/temperatures/types'

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}))

const reading: TemperatureReading = {
  id: 1,
  location: 'São Paulo',
  valueCelsius: 28.5,
  recordedAt: new Date('2024-01-15T14:30:00Z').toISOString(),
  isActive: true,
  createdAt: new Date().toISOString(),
  updatedAt: null,
}

describe('TemperatureCard', () => {
  it('renders the location', () => {
    render(<TemperatureCard reading={reading} />)
    expect(screen.getByText('São Paulo')).toBeInTheDocument()
  })

  it('renders the formatted temperature', () => {
    render(<TemperatureCard reading={reading} />)
    expect(screen.getByText('28.5°C')).toBeInTheDocument()
  })

  it('renders the i18n key for recordedAt label', () => {
    render(<TemperatureCard reading={reading} />)
    expect(screen.getByText(/temperature.recordedAt/)).toBeInTheDocument()
  })
})
