import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useAuth } from '@/stores/auth/hooks'
import { useDeleteMyData, useRecordConsent } from '@/services/consent/actions'
import { Button } from '@/components/ui/Button'

const SECTIONS = ['collected', 'purpose', 'legalBasis', 'retention', 'rights', 'exercise'] as const

export default function PrivacyPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { isAuthenticated } = useAuth()
  const recordConsent = useRecordConsent()
  const deleteMyData = useDeleteMyData()
  const [feedback, setFeedback] = useState<{ kind: 'ok' | 'err'; text: string } | null>(null)

  const withdraw = () => {
    recordConsent.mutate('withdrawn', {
      onSuccess: () => setFeedback({ kind: 'ok', text: t('privacy.withdrawn') }),
      onError: () => setFeedback({ kind: 'err', text: t('common.error') }),
    })
  }

  const deleteData = () => {
    deleteMyData.mutate(undefined, {
      onSuccess: (res) => setFeedback({ kind: 'ok', text: t('privacy.deleted', { count: res.data ?? 0 }) }),
      onError: () => setFeedback({ kind: 'err', text: t('common.error') }),
    })
  }

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      <header className="flex items-center justify-between border-b border-gray-200 bg-white px-6 py-4 dark:border-gray-700 dark:bg-gray-800">
        <h1 className="text-xl font-bold text-gray-900 dark:text-gray-100">{t('privacy.title')}</h1>
        <Button variant="secondary" onClick={() => navigate('/projects')}>{t('privacy.back')}</Button>
      </header>

      <main className="mx-auto max-w-3xl px-6 py-8">
        <p className="mb-6 text-sm text-gray-500 dark:text-gray-400">{t('privacy.intro')}</p>

        <div className="space-y-6">
          {SECTIONS.map((s) => (
            <section key={s}>
              <h2 className="mb-1 text-base font-semibold text-gray-900 dark:text-gray-100">
                {t(`privacy.sections.${s}.title`)}
              </h2>
              <p className="text-sm leading-relaxed text-gray-700 dark:text-gray-300">
                {t(`privacy.sections.${s}.body`)}
              </p>
            </section>
          ))}
        </div>

        {isAuthenticated && (
          <div className="mt-10 rounded-lg border border-gray-200 bg-white p-5 dark:border-gray-700 dark:bg-gray-800">
            <h2 className="mb-1 text-base font-semibold text-gray-900 dark:text-gray-100">{t('privacy.manage.title')}</h2>
            <p className="mb-4 text-sm text-gray-600 dark:text-gray-400">{t('privacy.manage.description')}</p>
            <div className="flex flex-wrap gap-3">
              <Button variant="secondary" onClick={withdraw} loading={recordConsent.isPending}>
                {t('privacy.manage.withdraw')}
              </Button>
              <Button variant="danger" onClick={deleteData} loading={deleteMyData.isPending}>
                {t('privacy.manage.delete')}
              </Button>
            </div>
            {feedback && (
              <p className={`mt-3 text-sm ${feedback.kind === 'ok' ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400'}`}>
                {feedback.text}
              </p>
            )}
          </div>
        )}
      </main>
    </div>
  )
}
