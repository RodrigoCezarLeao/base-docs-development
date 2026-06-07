import { useCallback, useMemo } from 'react'
import {
  useNodesState,
  useEdgesState,
  addEdge,
  type Node,
  type Edge,
  type Connection,
  type NodeDragHandler,
  type NodeMouseHandler,
} from 'reactflow'
import { useDocuments } from '@/services/documents/queries'
import { useConnections } from '@/services/connections/queries'
import { useCreateDocument } from '@/services/documents/actions'
import { useDeleteDocument, useUpdatePosition } from '@/services/documents/actions'
import { useCreateConnection, useDeleteConnection } from '@/services/connections/actions'
import { useCanvasSelection } from '@/stores/canvas/hooks'
import type { DocumentDto } from '@/services/documents/types'
import type { ConnectionDto } from '@/services/connections/types'

function docToNode(doc: DocumentDto, onDelete: (id: string) => void): Node {
  return {
    id: String(doc.id),
    type: 'documentNode',
    position: { x: doc.canvasX, y: doc.canvasY },
    data: {
      label: doc.title,
      filePath: doc.filePath,
      onDelete,
    },
  }
}

function connToEdge(conn: ConnectionDto): Edge {
  return {
    id: String(conn.id),
    source: String(conn.sourceDocumentId),
    target: String(conn.targetDocumentId),
    label: conn.label ?? undefined,
  }
}

export function useCanvas(projectId: number) {
  const { data: documents = [], isLoading: docsLoading } = useDocuments(projectId)
  const { data: connections = [], isLoading: connsLoading } = useConnections(projectId)

  const { mutate: createDocument } = useCreateDocument(projectId)
  const { mutate: deleteDocumentMutate } = useDeleteDocument(projectId)
  const { mutate: updatePosition } = useUpdatePosition(projectId)
  const { mutate: createConnection } = useCreateConnection(projectId)
  const { mutate: deleteConnectionMutate } = useDeleteConnection(projectId)

  const { selectDocument, clearSelection } = useCanvasSelection()

  const deleteDocument = useCallback(
    (nodeId: string) => {
      deleteDocumentMutate(Number(nodeId))
      clearSelection()
    },
    [deleteDocumentMutate, clearSelection],
  )

  const initialNodes = useMemo(
    () => documents.map((d) => docToNode(d, deleteDocument)),
    [documents, deleteDocument],
  )

  const initialEdges = useMemo(
    () => connections.map(connToEdge),
    [connections],
  )

  const [nodes, setNodes, onNodesChange] = useNodesState(initialNodes)
  const [edges, setEdges, onEdgesChange] = useEdgesState(initialEdges)

  // Sync nodes when documents change
  useMemo(() => {
    setNodes(documents.map((d) => docToNode(d, deleteDocument)))
  }, [documents, deleteDocument, setNodes])

  // Sync edges when connections change
  useMemo(() => {
    setEdges(connections.map(connToEdge))
  }, [connections, setEdges])

  const onConnect = useCallback(
    (params: Connection) => {
      if (!params.source || !params.target) return
      createConnection({
        sourceDocumentId: Number(params.source),
        targetDocumentId: Number(params.target),
      })
      setEdges((eds) => addEdge(params, eds))
    },
    [createConnection, setEdges],
  )

  const onNodeDragStop: NodeDragHandler = useCallback(
    (_event, node) => {
      updatePosition({
        id: Number(node.id),
        dto: { canvasX: node.position.x, canvasY: node.position.y },
      })
    },
    [updatePosition],
  )

  const onNodeClick: NodeMouseHandler = useCallback(
    (_event, node) => {
      selectDocument(Number(node.id))
    },
    [selectDocument],
  )

  const deleteConnection = useCallback(
    (edgeId: string) => {
      deleteConnectionMutate(Number(edgeId))
    },
    [deleteConnectionMutate],
  )

  return {
    nodes,
    edges,
    onNodesChange,
    onEdgesChange,
    onConnect,
    onNodeClick,
    onNodeDragStop,
    createDocument,
    deleteDocument,
    deleteConnection,
    isLoading: docsLoading || connsLoading,
  }
}
