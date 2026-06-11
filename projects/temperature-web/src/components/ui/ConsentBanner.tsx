import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { consentStorage } from '@/lib/consent'
import { useRecordConsent } from '@/services/consent/actions'
import { Button } from './Button'

/**
 * Global LGPD consent banner. Shown only while no decision is stored.
 * Accepting/refusing records the decision (locally + server-side) and hides the banner.
 * Until "Accept" is clicked, no X-Tracking-Consent header is ever sent.
 */
export function ConsentBanner() {
  const { t } = useTranslation()
  const recordConsent = useRecordConsent()
  const [decided, setDecided] = useState(() => consentStorage.get() !== null)

  if (decided) return null

  const decide = (decision: 'granted' | 'denied') => {
    // The server call is best-effort proof-of-consent; local state is what gates the header.
    recordConsent.mutate(decision)
    setDecided(true)
  }

  return (
    <div className="fixed inset-x-0 bottom-0 z-[60] border-t border-gray-200 bg-white/95 px-4 py-4 shadow-lg backdrop-blur dark:border-gray-700 dark:bg-gray-800/95">
      <div className="mx-auto flex max-w-4xl flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <p className="text-sm text-gray-700 dark:text-gray-300">
          {t('consent.message')}{' '}
          <Link to="/privacy" className="font-medium text-blue-600 underline hover:text-blue-700 dark:text-blue-400">
            {t('consent.learnMore')}
          </Link>
        </p>
        <div className="flex shrink-0 gap-2">
          <Button variant="secondary" onClick={() => decide('denied')}>{t('consent.reject')}</Button>
          <Button onClick={() => decide('granted')}>{t('consent.accept')}</Button>
        </div>
      </div>
    </div>
  )
}
