export type Theme = 'system' | 'light' | 'dark'
export type Language = 'pt-BR' | 'en'

export const THEMES: readonly Theme[] = ['system', 'light', 'dark']
export const LANGUAGES: readonly Language[] = ['pt-BR', 'en']

export function resolveDark(theme: Theme): boolean {
  if (theme === 'system') return window.matchMedia('(prefers-color-scheme: dark)').matches
  return theme === 'dark'
}

/** Toggles the `.dark` class on <html> based on the chosen theme. */
export function applyTheme(theme: Theme): void {
  document.documentElement.classList.toggle('dark', resolveDark(theme))
}

/** Reflects a setting in the URL query string (shareable; read on next load). */
export function setUrlParam(key: string, value: string): void {
  const url = new URL(window.location.href)
  url.searchParams.set(key, value)
  window.history.replaceState(null, '', url)
}

export function readUrlParam<T extends string>(key: string, allowed: readonly T[]): T | null {
  const value = new URLSearchParams(window.location.search).get(key)
  return value && (allowed as readonly string[]).includes(value) ? (value as T) : null
}
