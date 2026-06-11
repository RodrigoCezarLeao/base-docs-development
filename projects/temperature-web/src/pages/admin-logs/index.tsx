import { useEffect, useState } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useAuth } from '@/stores/auth/hooks'
import { useLogFiles, useLogs } from '@/services/logs/queries'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { Spinner } from '@/components/ui/Spinner'
import { Pagination } from '@/components/ui/Pagination'

const LEVELS = ['', 'INFO', 'WARN', 'ERROR', 'DEBUG', 'CRIT']

const levelColor: Record<string, string> = {
  INFO: 'text-blue-700 bg-blue-50 dark:text-blue-300 dark:bg-blue-500/15',
  WARN: 'text-amber-700 bg-amber-50 dark:text-amber-300 dark:bg-amber-500/15',
  ERROR: 'text-red-700 bg-red-50 dark:text-red-300 dark:bg-red-500/15',
  CRIT: 'text-red-800 bg-red-100 dark:text-red-200 dark:bg-red-500/25',
  DEBUG: 'text-gray-600 bg-gray-100 dark:text-gray-300 dark:bg-gray-700',
}

export default function AdminLogsPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { isAuthenticated, isAdmin } = useAuth()

  const { data: files = [] } = useLogFiles()
  const [date, setDate] = useState('')
  const [level, setLevel] = useState('')
  const [q, setQ] = useState('')
  const [page, setPage] = useState(1)

  useEffect(() => {
    if (!date && files.length > 0) setDate(files[0])
  }, [files, date])

  const { data, isFetching } = useLogs({ date, level: level || undefined, q: q || undefined, page, pageSize: 50 })

  if (!isAuthenticated) return <Navigate to="/login" replace />
  if (!isAdmin) return <Navigate to="/" replace />

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      <header className="bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 px-6 py-4 flex items-center justify-between">
        <h1 className="text-xl font-bold text-gray-900 dark:text-gray-100">{t('logs.title')}</h1>
        <Button variant="secondary" onClick={() => navigate('/')}>{t('logs.back')}</Button>
      </header>

      <main className="max-w-6xl mx-auto px-6 py-6">
        <div className="flex flex-wrap items-end gap-3 mb-4">
          <label className="flex flex-col gap-1 text-sm">
            <span className="font-medium text-gray-700 dark:text-gray-300">{t('logs.date')}</span>
            <select
              value={date}
              onChange={(e) => { setDate(e.target.value); setPage(1) }}
              className="px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md text-sm bg-white text-gray-900 dark:bg-gray-800 dark:text-gray-100"
            >
              {files.length === 0 && <option value="">—</option>}
              {files.map((f) => <option key={f} value={f}>{f}</option>)}
            </select>
          </label>
          <label className="flex flex-col gap-1 text-sm">
            <span className="font-medium text-gray-700 dark:text-gray-300">{t('logs.level')}</span>
            <select
              value={level}
              onChange={(e) => { setLevel(e.target.value); setPage(1) }}
              className="px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md text-sm bg-white text-gray-900 dark:bg-gray-800 dark:text-gray-100"
            >
              {LEVELS.map((l) => <option key={l} value={l}>{l || t('logs.allLevels')}</option>)}
            </select>
          </label>
          <div className="flex-1 min-w-[12rem]">
            <Input
              label={t('logs.search')}
              value={q}
              onChange={(e) => { setQ(e.target.value); setPage(1) }}
              placeholder={t('logs.searchPlaceholder')}
            />
          </div>
        </div>

        <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-gray-50 dark:bg-gray-900/40 text-left text-gray-500 dark:text-gray-400">
                <tr>
                  <th className="px-3 py-2 font-medium whitespace-nowrap">{t('logs.timestamp')}</th>
                  <th className="px-3 py-2 font-medium">{t('logs.level')}</th>
                  <th className="px-3 py-2 font-medium">{t('logs.category')}</th>
                  <th className="px-3 py-2 font-medium">{t('logs.user')}</th>
                  <th className="px-3 py-2 font-medium">{t('logs.message')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100 dark:divide-gray-700">
                {data?.items.map((entry, i) => (
                  <tr key={`${entry.timestamp}-${i}`} className="align-top">
                    <td className="px-3 py-2 font-mono text-xs text-gray-500 dark:text-gray-400 whitespace-nowrap">{entry.timestamp}</td>
                    <td className="px-3 py-2">
                      <span className={`rounded px-1.5 py-0.5 text-xs font-medium ${levelColor[entry.level] ?? 'text-gray-600 bg-gray-100 dark:text-gray-300 dark:bg-gray-700'}`}>
                        {entry.level}
                      </span>
                    </td>
                    <td className="px-3 py-2 font-mono text-xs text-gray-600 dark:text-gray-400">{entry.category}</td>
                    <td className="px-3 py-2 text-xs text-gray-600 dark:text-gray-400">{entry.userId === '-' ? '' : entry.userId}</td>
                    <td className="px-3 py-2 text-gray-800 dark:text-gray-200 break-words">{entry.message}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {isFetching && <div className="flex justify-center py-8"><Spinner /></div>}
          {!isFetching && (data?.items.length ?? 0) === 0 && (
            <p className="text-center text-gray-500 dark:text-gray-400 py-8">{t('logs.empty')}</p>
          )}
        </div>

        {data && <Pagination page={data.page} totalPages={data.totalPages} onPageChange={setPage} />}
      </main>
    </div>
  )
}
