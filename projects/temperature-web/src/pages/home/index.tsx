import { useTranslation } from 'react-i18next'
import { PageHeader } from '@/components/shared/PageHeader'
import { TemperatureList, TemperatureFilters, AddTemperatureForm, useTemperatureList } from '@/features/temperature-list'

export function HomePage() {
  const { t } = useTranslation()
  const {
    readings, isLoading, isError,
    page, totalPages, setPage,
    locationFilter, setLocationFilter, clearFilters,
    handleCreate, isCreating,
  } = useTemperatureList()

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
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
