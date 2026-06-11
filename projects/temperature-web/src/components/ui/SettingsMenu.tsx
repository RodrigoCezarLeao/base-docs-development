import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { cn } from '@/lib/cn'
import { LANGUAGES, THEMES } from '@/lib/settings'
import { useSettings } from '@/stores/settings/store'

/** Gear button → dropdown with general settings (theme + language). Rendered globally. */
export function SettingsMenu() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { theme, language, setTheme, setLanguage } = useSettings()
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)

  useEffect(() => {
    function onClickOutside(event: MouseEvent) {
      if (ref.current && !ref.current.contains(event.target as Node)) setOpen(false)
    }
    document.addEventListener('mousedown', onClickOutside)
    return () => document.removeEventListener('mousedown', onClickOutside)
  }, [])

  const option = (active: boolean) =>
    cn(
      'text-left rounded px-2 py-1',
      active
        ? 'bg-blue-50 text-blue-700 dark:bg-blue-500/20 dark:text-blue-300'
        : 'text-gray-700 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-700',
    )

  return (
    <div ref={ref} className="fixed top-3 right-3 z-50">
      <button
        type="button"
        aria-label={t('settings.title')}
        onClick={() => setOpen((o) => !o)}
        className="rounded-full border border-gray-200 bg-white/80 p-2 text-gray-600 shadow-sm backdrop-blur hover:bg-white dark:border-gray-700 dark:bg-gray-800/80 dark:text-gray-300 dark:hover:bg-gray-800"
      >
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
          <circle cx="12" cy="12" r="3" />
          <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z" />
        </svg>
      </button>

      {open && (
        <div className="absolute right-0 mt-2 w-48 rounded-lg border border-gray-200 bg-white p-3 text-sm shadow-lg dark:border-gray-700 dark:bg-gray-800">
          <p className="mb-1 font-medium text-gray-500 dark:text-gray-400">{t('settings.theme')}</p>
          <div className="mb-3 flex flex-col gap-0.5">
            {THEMES.map((th) => (
              <button key={th} type="button" className={option(theme === th)} onClick={() => setTheme(th)}>
                {t(`settings.themes.${th}`)}
              </button>
            ))}
          </div>
          <p className="mb-1 font-medium text-gray-500 dark:text-gray-400">{t('settings.language')}</p>
          <div className="flex flex-col gap-0.5">
            {LANGUAGES.map((lg) => (
              <button key={lg} type="button" className={option(language === lg)} onClick={() => setLanguage(lg)}>
                {t(`settings.languages.${lg}`)}
              </button>
            ))}
          </div>
          <div className="mt-3 border-t border-gray-100 pt-2 dark:border-gray-700">
            <button
              type="button"
              className="w-full rounded px-2 py-1 text-left text-gray-700 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-700"
              onClick={() => { setOpen(false); navigate('/privacy') }}
            >
              {t('settings.privacy')}
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
