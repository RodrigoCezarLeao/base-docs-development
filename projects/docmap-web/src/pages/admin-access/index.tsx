import { useState } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useAuth } from '@/stores/auth/hooks'
import { useAccessEvents } from '@/services/access/queries'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { Spinner } from '@/components/ui/Spinner'
import { Pagination } from '@/components/ui/Pagination'

export default function AdminAccessPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { isAuthenticated, isAdmin } = useAuth()

  const [q, setQ] = useState('')
  const [userId, setUserId] = useState('')
  const [page, setPage] = useState(1)

  const { data, isFetching } = useAccessEvents({
    q: q || undefined,
    userId: userId ? Number(userId) : undefined,
    page,
    pageSize: 50,
  })

  if (!isAuthenticated) return <Navigate to="/login" replace />
  if (!isAdmin) return <Navigate to="/projects" replace />

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      <header className="flex items-center justify-between border-b border-gray-200 bg-white px-6 py-4 dark:border-gray-700 dark:bg-gray-800">
        <h1 className="text-xl font-bold text-gray-900 dark:text-gray-100">{t('access.title')}</h1>
        <Button variant="secondary" onClick={() => navigate('/projects')}>{t('access.back')}</Button>
      </header>

      <main className="mx-auto max-w-6xl px-6 py-6">
        <div className="mb-4 flex flex-wrap items-end gap-3">
          <div className="w-32">
            <Input
              label={t('access.userId')}
              value={userId}
              onChange={(e) => { setUserId(e.target.value.replace(/\D/g, '')); setPage(1) }}
              placeholder="—"
            />
          </div>
          <div className="min-w-[12rem] flex-1">
            <Input
              label={t('access.search')}
              value={q}
              onChange={(e) => { setQ(e.target.value); setPage(1) }}
              placeholder={t('access.searchPlaceholder')}
            />
          </div>
        </div>

        <div className="overflow-hidden rounded-lg border border-gray-200 bg-white dark:border-gray-700 dark:bg-gray-800">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-gray-50 text-left text-gray-500 dark:bg-gray-900/40 dark:text-gray-400">
                <tr>
                  <th className="whitespace-nowrap px-3 py-2 font-medium">{t('access.time')}</th>
                  <th className="px-3 py-2 font-medium">{t('access.user')}</th>
                  <th className="px-3 py-2 font-medium">{t('access.ip')}</th>
                  <th className="px-3 py-2 font-medium">{t('access.device')}</th>
                  <th className="px-3 py-2 font-medium">{t('access.request')}</th>
                  <th className="px-3 py-2 font-medium">{t('access.status')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100 dark:divide-gray-700">
                {data?.items.map((e) => (
                  <tr key={e.id} className="align-top">
                    <td className="whitespace-nowrap px-3 py-2 font-mono text-xs text-gray-500 dark:text-gray-400">
                      {new Date(e.occurredAt).toLocaleString()}
                    </td>
                    <td className="px-3 py-2 text-xs text-gray-600 dark:text-gray-400">{e.userId ?? '—'}</td>
                    <td className="px-3 py-2 font-mono text-xs text-gray-600 dark:text-gray-400">{e.ip}</td>
                    <td className="px-3 py-2 text-xs text-gray-600 dark:text-gray-400">
                      {[e.browser, e.os, e.deviceType].filter(Boolean).join(' · ')}
                    </td>
                    <td className="px-3 py-2 font-mono text-xs text-gray-800 dark:text-gray-200">
                      {e.method} {e.path}
                    </td>
                    <td className="px-3 py-2 text-xs text-gray-600 dark:text-gray-400">{e.statusCode}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {isFetching && <div className="flex justify-center py-8"><Spinner /></div>}
          {!isFetching && (data?.items.length ?? 0) === 0 && (
            <p className="py-8 text-center text-gray-500 dark:text-gray-400">{t('access.empty')}</p>
          )}
        </div>

        {data && <Pagination page={data.page} totalPages={data.totalPages} onPageChange={setPage} />}
      </main>
    </div>
  )
}
