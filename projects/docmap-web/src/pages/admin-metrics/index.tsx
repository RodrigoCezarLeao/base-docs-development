import { Navigate, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useAuth } from '@/stores/auth/hooks'
import { useMetrics } from '@/services/metrics/queries'
import { Button } from '@/components/ui/Button'
import { TrafficChart } from '@/components/ui/TrafficChart'

function Stat({ label, value }: { label: string; value: number }) {
  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-4">
      <p className="text-sm text-gray-500 dark:text-gray-400">{label}</p>
      <p className="text-3xl font-bold text-gray-900 dark:text-gray-100 tabular-nums">{value}</p>
    </div>
  )
}

export default function AdminMetricsPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { isAuthenticated, isAdmin } = useAuth()
  const { data } = useMetrics()

  if (!isAuthenticated) return <Navigate to="/login" replace />
  if (!isAdmin) return <Navigate to="/projects" replace />

  const traffic = data?.traffic.map((p) => p.count) ?? []

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      <header className="bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 px-6 py-4 flex items-center justify-between">
        <h1 className="text-xl font-bold text-gray-900 dark:text-gray-100">{t('metrics.title')}</h1>
        <Button variant="secondary" onClick={() => navigate('/projects')}>{t('metrics.back')}</Button>
      </header>

      <main className="max-w-6xl mx-auto px-6 py-6 flex flex-col gap-6">
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <Stat label={t('metrics.activeUsers')} value={data?.activeUsers ?? 0} />
          <Stat label={t('metrics.inFlight')} value={data?.inFlight ?? 0} />
          <Stat label={t('metrics.total')} value={data?.totalRequests ?? 0} />
        </div>

        <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-4">
          <p className="text-sm font-medium text-gray-500 dark:text-gray-400 mb-2">{t('metrics.traffic')}</p>
          <TrafficChart data={traffic} />
        </div>

        <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 dark:bg-gray-900/40 text-left text-gray-500 dark:text-gray-400">
              <tr>
                <th className="px-3 py-2 font-medium">{t('metrics.endpoint')}</th>
                <th className="px-3 py-2 font-medium text-right">{t('metrics.count')}</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100 dark:divide-gray-700">
              {data?.endpoints.map((e) => (
                <tr key={e.endpoint}>
                  <td className="px-3 py-2 font-mono text-xs text-gray-700 dark:text-gray-300 break-all">{e.endpoint}</td>
                  <td className="px-3 py-2 text-right tabular-nums text-gray-900 dark:text-gray-100">{e.count}</td>
                </tr>
              ))}
              {(data?.endpoints.length ?? 0) === 0 && (
                <tr>
                  <td colSpan={2} className="px-3 py-8 text-center text-gray-500 dark:text-gray-400">{t('metrics.empty')}</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </main>
    </div>
  )
}
