import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useCanvasSelection } from '@/stores/canvas/hooks'
import { useDocument } from '@/services/documents/queries'
import { useUpdateDocument } from '@/services/documents/actions'
import { Input } from '@/components/ui/Input'
import { Spinner } from '@/components/ui/Spinner'

interface DocumentSidePanelProps {
  projectId: number
}

export function DocumentSidePanel({ projectId }: DocumentSidePanelProps) {
  const { t } = useTranslation()
  const { selectedDocumentId } = useCanvasSelection()
  const { data: document, isLoading } = useDocument(projectId, selectedDocumentId ?? 0)
  const { mutate: updateDocument } = useUpdateDocument(projectId)

  const [title, setTitle] = useState('')
  const [filePath, setFilePath] = useState('')
  const [content, setContent] = useState('')

  useEffect(() => {
    if (document) {
      setTitle(document.title)
      setFilePath(document.filePath)
      setContent(document.content)
    }
  }, [document])

  function handleBlur() {
    if (!document) return
    updateDocument({
      id: document.id,
      dto: { title, filePath, content },
    })
  }

  if (!selectedDocumentId) {
    return (
      <aside className="w-[380px] bg-white border-l border-gray-200 flex items-center justify-center">
        <p className="text-sm text-gray-400">{t('canvas.noSelection')}</p>
      </aside>
    )
  }

  if (isLoading) {
    return (
      <aside className="w-[380px] bg-white border-l border-gray-200 flex items-center justify-center">
        <Spinner />
      </aside>
    )
  }

  return (
    <aside className="w-[380px] bg-white border-l border-gray-200 flex flex-col">
      <div className="p-4 border-b border-gray-200 flex flex-col gap-3">
        <Input
          label={t('canvas.title')}
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          onBlur={handleBlur}
        />
        <Input
          label={t('canvas.filePath')}
          value={filePath}
          onChange={(e) => setFilePath(e.target.value)}
          onBlur={handleBlur}
        />
      </div>
      <div className="flex-1 flex flex-col p-4 gap-2">
        <label className="text-sm font-medium text-gray-700">{t('canvas.content')}</label>
        <textarea
          className="flex-1 resize-none border border-gray-300 rounded-md p-2 text-sm font-mono
            focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          value={content}
          onChange={(e) => setContent(e.target.value)}
          onBlur={handleBlur}
        />
      </div>
    </aside>
  )
}
