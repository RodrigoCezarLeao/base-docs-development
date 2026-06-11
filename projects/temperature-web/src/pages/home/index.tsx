import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import type { DriveStep } from 'driver.js'
import { PageHeader } from '@/components/shared/PageHeader'
import { Button } from '@/components/ui/Button'
import { useTour } from '@/hooks/useTour'
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

  const steps: DriveStep[] = [
    { popover: { title: t('tour.welcomeTitle'), description: t('tour.welcomeDesc') } },
    { element: '[data-tour="add-form"]', popover: { title: t('tour.addTitle'), description: t('tour.addDesc') } },
    { element: '[data-tour="filters"]', popover: { title: t('tour.filtersTitle'), description: t('tour.filtersDesc') } },
    { element: '[data-tour="list"]', popover: { title: t('tour.listTitle'), description: t('tour.listDesc') } },
  ]
  const { start: startTour } = useTour(steps, {
    tourId: 'temperature-home',
    labels: { next: t('tour.next'), prev: t('tour.prev'), done: t('tour.done') },
  })

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      <div className="flex justify-end gap-2 mb-4">
        <Button variant="secondary" onClick={startTour}>{t('tour.start')}</Button>
        {isAdmin && (
          <Button variant="secondary" onClick={() => navigate('/admin/logs')}>{t('logs.nav')}</Button>
        )}
        {isAdmin && (
          <Button variant="secondary" onClick={() => navigate('/admin/metrics')}>{t('metrics.nav')}</Button>
        )}
        {isAuthenticated ? (
          <Button variant="secondary" onClick={logout}>{t('common.logout')}</Button>
        ) : (
          <Button variant="secondary" onClick={() => navigate('/login')}>{t('auth.login')}</Button>
        )}
      </div>
      <PageHeader title={t('temperature.title')} />
      <div data-tour="add-form">
        <AddTemperatureForm onSubmit={handleCreate} isLoading={isCreating} />
      </div>
      <div data-tour="filters">
        <TemperatureFilters
          locationFilter={locationFilter}
          onLocationChange={setLocationFilter}
          onClear={clearFilters}
        />
      </div>
      <div data-tour="list">
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
    </div>
  )
}
