import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { useCreateDocument } from '@/services/documents/actions'
import { tokenStorage } from '@/lib/auth'

interface CanvasToolbarProps {
  projectId: number
  projectName: string
}

export function CanvasToolbar({ projectId, projectName }: CanvasToolbarProps) {
  const { t } = useTranslation()
  const { mutate: createDocument, isPending } = useCreateDocument(projectId)

  const [showModal, setShowModal] = useState(false)
  const [title, setTitle] = useState('')
  const [filePath, setFilePath] = useState('')

  function handleCreate(e: React.FormEvent) {
    e.preventDefault()
    if (!title.trim()) return

    createDocument(
      { title: title.trim(), filePath: filePath.trim() || `${title.trim()}.md`, canvasX: 100, canvasY: 100 },
      {
        onSuccess: () => {
          setShowModal(false)
          setTitle('')
          setFilePath('')
        },
      },
    )
  }

  function handleExport() {
    const token = tokenStorage.get()
    const url = `${import.meta.env.VITE_API_URL ?? 'http://localhost:5001'}/api/v1/projects/${projectId}/export`

    fetch(url, { headers: { Authorization: `Bearer ${token}` } })
      .then((res) => res.blob())
      .then((blob) => {
        const anchor = document.createElement('a')
        anchor.href = URL.createObjectURL(blob)
        anchor.download = `${projectName}.zip`
        anchor.click()
        URL.revokeObjectURL(anchor.href)
      })
      .catch(() => {
        window.alert(t('common.error'))
      })
  }

  return (
    <>
      <div className="absolute top-4 left-4 z-10 flex items-center gap-2 bg-white rounded-lg shadow-md px-3 py-2 border border-gray-200">
        <span className="font-semibold text-gray-800 text-sm mr-2">{projectName}</span>
        <Button onClick={() => setShowModal(true)}>
          {t('canvas.newDocument')}
        </Button>
        <Button variant="secondary" onClick={handleExport}>
          {t('projects.export')}
        </Button>
      </div>

      {showModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg shadow-xl p-6 w-full max-w-md">
            <h2 className="text-lg font-semibold mb-4">{t('canvas.newDocument')}</h2>
            <form onSubmit={handleCreate} className="flex flex-col gap-4">
              <Input
                label={t('canvas.title')}
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                required
                autoFocus
              />
              <Input
                label={t('canvas.filePath')}
                value={filePath}
                onChange={(e) => setFilePath(e.target.value)}
                placeholder="folder/doc.md"
              />
              <div className="flex gap-2 justify-end">
                <Button type="button" variant="secondary" onClick={() => setShowModal(false)}>
                  {t('common.cancel')}
                </Button>
                <Button type="submit" loading={isPending}>
                  {t('common.create')}
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}
    </>
  )
}
