export interface ConnectionDto {
  id: number
  projectId: number
  sourceDocumentId: number
  targetDocumentId: number
  label: string | null
  createdAt: string
}

export interface CreateConnectionDto {
  sourceDocumentId: number
  targetDocumentId: number
  label?: string
}
