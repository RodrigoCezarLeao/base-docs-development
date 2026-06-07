import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { useCreateProject } from '@/services/projects/actions'

interface CreateProjectModalProps {
  onClose: () => void
}

export function CreateProjectModal({ onClose }: CreateProjectModalProps) {
  const { t } = useTranslation()
  const { mutate: createProject, isPending } = useCreateProject()

  const [name, setName] = useState('')
  const [description, setDescription] = useState('')

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!name.trim()) return

    createProject(
      { name: name.trim(), description: description.trim() || undefined },
      { onSuccess: onClose },
    )
  }

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-xl p-6 w-full max-w-md">
        <h2 className="text-lg font-semibold mb-4">{t('projects.create')}</h2>
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <Input
            label={t('projects.name')}
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
            autoFocus
          />
          <Input
            label={t('projects.description')}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
          />
          <div className="flex gap-2 justify-end">
            <Button type="button" variant="secondary" onClick={onClose}>
              {t('common.cancel')}
            </Button>
            <Button type="submit" loading={isPending}>
              {t('common.create')}
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}
