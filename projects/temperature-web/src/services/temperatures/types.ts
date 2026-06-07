export interface TemperatureReading {
  id: number
  location: string
  valueCelsius: number
  recordedAt: string
  isActive: boolean
  createdAt: string
  updatedAt: string | null
}

export interface CreateTemperatureReadingDto {
  location: string
  valueCelsius: number
  recordedAt: string
}
