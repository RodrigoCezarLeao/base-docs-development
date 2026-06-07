import { useMemo } from 'react'
import { Navigate, useParams } from 'react-router-dom'
import ReactFlow, { Background, Controls, MiniMap } from 'reactflow'
import { useAuth } from '@/stores/auth/hooks'
import { useProject } from '@/services/projects/queries'
import { DocumentNode, DocumentSidePanel, CanvasToolbar, useCanvas } from '@/features/canvas'
import { Spinner } from '@/components/ui/Spinner'

export default function CanvasPage() {
  const { projectId } = useParams<{ projectId: string }>()
  const { isAuthenticated } = useAuth()
  const parsedProjectId = Number(projectId)

  const { data: project } = useProject(parsedProjectId)

  const {
    nodes,
    edges,
    onNodesChange,
    onEdgesChange,
    onConnect,
    onNodeClick,
    onNodeDragStop,
    deleteConnection,
    isLoading,
  } = useCanvas(parsedProjectId)

  const nodeTypes = useMemo(() => ({ documentNode: DocumentNode }), [])

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  if (isLoading) {
    return (
      <div className="flex h-screen items-center justify-center">
        <Spinner size="lg" />
      </div>
    )
  }

  return (
    <div className="flex" style={{ height: '100vh' }}>
      <div className="flex-1 relative">
        <ReactFlow
          nodes={nodes}
          edges={edges}
          nodeTypes={nodeTypes}
          onNodesChange={onNodesChange}
          onEdgesChange={onEdgesChange}
          onConnect={onConnect}
          onNodeClick={onNodeClick}
          onNodeDragStop={onNodeDragStop}
          onEdgeClick={(_event, edge) => deleteConnection(edge.id)}
          fitView
          className="w-full h-full"
        >
          <Background />
          <Controls />
          <MiniMap />
        </ReactFlow>

        <CanvasToolbar
          projectId={parsedProjectId}
          projectName={project?.name ?? ''}
        />
      </div>

      <DocumentSidePanel projectId={parsedProjectId} />
    </div>
  )
}
