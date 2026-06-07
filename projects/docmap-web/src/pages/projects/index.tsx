import { useState } from 'react'
import { Navigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useAuth } from '@/stores/auth/hooks'
import { useProjects } from '@/services/projects/queries'
import { ProjectCard, CreateProjectModal } from '@/features/projects'
import { Button } from '@/components/ui/Button'
import { Spinner } from '@/components/ui/Spinner'

export default function ProjectsPage() {
  const { t } = useTranslation()
  const { isAuthenticated, logout } = useAuth()
  const { data: projects = [], isLoading } = useProjects()
  const [showModal, setShowModal] = useState(false)

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white border-b border-gray-200 px-6 py-4 flex items-center justify-between">
        <h1 className="text-xl font-bold text-gray-900">DocMap</h1>
        <div className="flex items-center gap-3">
          <Button onClick={() => setShowModal(true)}>
            {t('projects.create')}
          </Button>
          <Button variant="secondary" onClick={logout}>
            {t('common.logout')}
          </Button>
        </div>
      </header>

      <main className="max-w-6xl mx-auto px-6 py-8">
        <h2 className="text-2xl font-semibold text-gray-900 mb-6">
          {t('projects.title')}
        </h2>

        {isLoading && (
          <div className="flex justify-center py-12">
            <Spinner size="lg" />
          </div>
        )}

        {!isLoading && projects.length === 0 && (
          <p className="text-center text-gray-500 py-12">{t('projects.empty')}</p>
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
