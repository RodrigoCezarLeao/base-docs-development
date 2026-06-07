import { describe, it, expect } from 'vitest'
import { formatCelsius, celsiusToFahrenheit } from './format'

describe('formatCelsius', () => {
  it('formats with one decimal place and degree symbol', () => {
    expect(formatCelsius(36.6)).toBe('36.6°C')
  })

  it('rounds to one decimal', () => {
    expect(formatCelsius(100)).toBe('100.0°C')
  })

  it('handles negative values', () => {
    expect(formatCelsius(-5.5)).toBe('-5.5°C')
  })
})

describe('celsiusToFahrenheit', () => {
  it('converts 0°C to 32°F', () => {
    expect(celsiusToFahrenheit(0)).toBe(32)
  })

  it('converts 100°C to 212°F', () => {
    expect(celsiusToFahrenheit(100)).toBe(212)
  })

  it('converts 37°C to 99°F', () => {
    expect(celsiusToFahrenheit(37)).toBe(99)
  })
})
