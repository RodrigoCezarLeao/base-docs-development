import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { PageHeader } from '@/components/shared/PageHeader'
import { Button } from '@/components/ui/Button'
import { useAuth } from '@/stores/auth/hooks'
import { TemperatureList, TemperatureFilters, AddTemperatureForm, useTemperatureList } from '@/features/temperature-list'

export function HomePage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { isAuthenticated, isAdmin, logout } = useAuth()
  const {
    readings, isLoading, isError,
    page, totalPages, setPage,
    locationFilter, setLocationFilter, clearFilters,
    handleCreate, isCreating,
  } = useTemperatureList()

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      <div className="flex justify-end gap-2 mb-4">
        {isAdmin && (
          <Button variant="secondary" onClick={() => navigate('/admin/logs')}>{t('logs.nav')}</Button>
        )}
        {isAuthenticated ? (
          <Button variant="secondary" onClick={logout}>{t('common.logout')}</Button>
        ) : (
          <Button variant="secondary" onClick={() => navigate('/login')}>{t('auth.login')}</Button>
        )}
      </div>
      <PageHeader title={t('temperature.title')} />
      <AddTemperatureForm onSubmit={handleCreate} isLoading={isCreating} />
      <TemperatureFilters
        locationFilter={locationFilter}
        onLocationChange={setLocationFilter}
        onClear={clearFilters}
      />
      <TemperatureList
        readings={readings}
        isLoading={isLoading}
        isError={isError}
        page={page}
        totalPages={totalPages}
        onPageChange={setPage}
        locationFilter={locationFilter}
      />
    </div>
  )
}
