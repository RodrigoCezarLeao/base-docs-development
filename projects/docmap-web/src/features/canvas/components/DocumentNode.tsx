import { memo } from 'react'
import { Handle, Position } from 'reactflow'
import { cn } from '@/lib/cn'

interface DocumentNodeData {
  label: string
  filePath: string
  selected: boolean
  onDelete: (id: string) => void
}

interface DocumentNodeProps {
  id: string
  data: DocumentNodeData
  selected: boolean
}

function DocumentNodeComponent({ id, data, selected }: DocumentNodeProps) {
  function handleDelete(e: React.MouseEvent) {
    e.stopPropagation()
    data.onDelete(id)
  }

  return (
    <div
      className={cn(
        'bg-white rounded-lg border-2 px-4 py-3 min-w-[160px] max-w-[240px] shadow-sm',
        selected ? 'border-blue-500' : 'border-gray-200',
      )}
    >
      <Handle type="target" position={Position.Top} />
      <Handle type="target" position={Position.Left} />

      <div className="flex items-start justify-between gap-2">
        <div className="flex-1 min-w-0">
          <p className="font-medium text-gray-900 text-sm truncate">{data.label}</p>
          <p className="text-xs text-gray-400 truncate mt-0.5">{data.filePath}</p>
        </div>
        <button
          onClick={handleDelete}
          className="text-gray-400 hover:text-red-500 text-xs leading-none flex-shrink-0 mt-0.5"
          title="Excluir nó"
        >
          ×
        </button>
      </div>

      <Handle type="source" position={Position.Bottom} />
      <Handle type="source" position={Position.Right} />
    </div>
  )
}

export const DocumentNode = memo(DocumentNodeComponent)
