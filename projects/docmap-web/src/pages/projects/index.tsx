import { useState } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import type { DriveStep } from 'driver.js'
import { useAuth } from '@/stores/auth/hooks'
import { useProjects } from '@/services/projects/queries'
import { ProjectCard, CreateProjectModal } from '@/features/projects'
import { Button } from '@/components/ui/Button'
import { Spinner } from '@/components/ui/Spinner'
import { useTour } from '@/hooks/useTour'

export default function ProjectsPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { isAuthenticated, isAdmin, logout } = useAuth()
  const { data: projects = [], isLoading } = useProjects()
  const [showModal, setShowModal] = useState(false)

  const steps: DriveStep[] = [
    { popover: { title: t('tour.welcomeTitle'), description: t('tour.welcomeDesc') } },
    { element: '[data-tour="create"]', popover: { title: t('tour.createTitle'), description: t('tour.createDesc') } },
    { element: '[data-tour="projects"]', popover: { title: t('tour.listTitle'), description: t('tour.listDesc') } },
  ]
  const { start: startTour } = useTour(steps, {
    tourId: 'docmap-projects',
    labels: { next: t('tour.next'), prev: t('tour.prev'), done: t('tour.done') },
  })

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      <header className="bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 px-6 py-4 flex items-center justify-between">
        <h1 className="text-xl font-bold text-gray-900 dark:text-gray-100">DocMap</h1>
        <div className="flex items-center gap-3">
          <span data-tour="create">
            <Button onClick={() => setShowModal(true)}>
              {t('projects.create')}
            </Button>
          </span>
          {isAdmin && (
            <Button variant="secondary" onClick={() => navigate('/admin/logs')}>
              {t('logs.nav')}
            </Button>
          )}
          {isAdmin && (
            <Button variant="secondary" onClick={() => navigate('/admin/metrics')}>
              {t('metrics.nav')}
            </Button>
          )}
          <Button variant="secondary" onClick={startTour}>{t('tour.start')}</Button>
          <Button variant="secondary" onClick={logout}>
            {t('common.logout')}
          </Button>
        </div>
      </header>

      <main className="max-w-6xl mx-auto px-6 py-8" data-tour="projects">
        <h2 className="text-2xl font-semibold text-gray-900 dark:text-gray-100 mb-6">
          {t('projects.title')}
        </h2>

        {isLoading && (
          <div className="flex justify-center py-12">
            <Spinner size="lg" />
          </div>
        )}

        {!isLoading && projects.length === 0 && (
          <p className="text-center text-gray-500 dark:text-gray-400 py-12">{t('projects.empty')}</p>
        )}

        {!isLoading && projects.length > 0 && (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {projects.map((project) => (
              <ProjectCard key={project.id} project={project} />
            ))}
          </div>
        )}
      </main>

      {showModal && <CreateProjectModal onClose={() => setShowModal(false)} />}
    </div>
  )
}
