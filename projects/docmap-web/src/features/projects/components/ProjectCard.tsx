import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/Button'
import { useDeleteProject } from '@/services/projects/actions'
import { tokenStorage } from '@/lib/auth'
import type { ProjectDto } from '@/services/projects/types'

interface ProjectCardProps {
  project: ProjectDto
}

export function ProjectCard({ project }: ProjectCardProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { mutate: deleteProject, isPending: isDeleting } = useDeleteProject()

  function handleExport() {
    const token = tokenStorage.get()
    const url = `${import.meta.env.VITE_API_URL ?? 'http://localhost:5001'}/api/v1/projects/${project.id}/export`

    fetch(url, { headers: { Authorization: `Bearer ${token}` } })
      .then((res) => res.blob())
      .then((blob) => {
        const anchor = document.createElement('a')
        anchor.href = URL.createObjectURL(blob)
        anchor.download = `${project.name}.zip`
        anchor.click()
        URL.revokeObjectURL(anchor.href)
      })
      .catch(() => {
        window.alert(t('common.error'))
      })
  }

  function handleDelete() {
    if (window.confirm(`${t('projects.delete')} "${project.name}"?`)) {
      deleteProject(project.id)
    }
  }

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-4 flex flex-col gap-3 shadow-sm">
      <div>
        <h3 className="font-semibold text-gray-900 dark:text-gray-100 text-lg">{project.name}</h3>
        {project.description && (
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">{project.description}</p>
        )}
        <p className="text-xs text-gray-400 dark:text-gray-500 mt-1">
          {project.documentCount} {t('projects.documents')}
        </p>
      </div>
      <div className="flex gap-2 flex-wrap">
        <Button variant="primary" onClick={() => navigate(`/canvas/${project.id}`)}>
          Abrir
        </Button>
        <Button variant="secondary" onClick={handleExport}>
          {t('projects.export')}
        </Button>
        <Button variant="danger" onClick={handleDelete} loading={isDeleting}>
          {t('projects.delete')}
        </Button>
      </div>
    </div>
  )
}
