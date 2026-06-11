import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import i18n from '@/i18n'
import {
  applyTheme,
  readUrlParam,
  setUrlParam,
  LANGUAGES,
  THEMES,
  type Language,
  type Theme,
} from '@/lib/settings'

interface SettingsStore {
  theme: Theme
  language: Language
  setTheme: (theme: Theme) => void
  setLanguage: (language: Language) => void
}

export const useSettings = create<SettingsStore>()(
  persist(
    (set) => ({
      theme: 'system',
      language: 'pt-BR',
      setTheme: (theme) => {
        set({ theme })
        applyTheme(theme)
        setUrlParam('theme', theme)
      },
      setLanguage: (language) => {
        set({ language })
        void i18n.changeLanguage(language)
        setUrlParam('lang', language)
      },
    }),
    { name: 'temperature-settings' },
  ),
)

/**
 * Call once at startup (in main.tsx) before render. URL params (?theme=&lang=) override
 * the persisted preferences; then the theme + language are applied without a flash.
 */
export function initSettings(): void {
  const store = useSettings.getState()
  const urlTheme = readUrlParam('theme', THEMES)
  const urlLang = readUrlParam('lang', LANGUAGES)
  if (urlTheme) store.setTheme(urlTheme)
  if (urlLang) store.setLanguage(urlLang)

  applyTheme(useSettings.getState().theme)
  void i18n.changeLanguage(useSettings.getState().language)

  // Follow OS changes while in "system" mode.
  window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
    if (useSettings.getState().theme === 'system') applyTheme('system')
  })
}
