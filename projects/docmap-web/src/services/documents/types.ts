export interface DocumentDto {
  id: number
  projectId: number
  title: string
  filePath: string
  content: string
  canvasX: number
  canvasY: number
  createdAt: string
  updatedAt: string | null
}

export interface CreateDocumentDto {
  title: string
  filePath: string
  content?: string
  canvasX?: number
  canvasY?: number
}

export interface UpdateDocumentDto {
  title: string
  filePath: string
  content: string
}

export interface UpdatePositionDto {
  canvasX: number
  canvasY: number
}
