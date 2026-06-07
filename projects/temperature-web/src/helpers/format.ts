export function formatCelsius(value: number): string {
  return `${value.toFixed(1)}°C`
}

export function formatDateTime(isoString: string, locale = 'pt-BR'): string {
  return new Intl.DateTimeFormat(locale, {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(new Date(isoString))
}

export function celsiusToFahrenheit(celsius: number): number {
  return Math.round((celsius * 9) / 5 + 32)
}
