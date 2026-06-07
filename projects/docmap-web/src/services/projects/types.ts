export interface ProjectDto {
  id: number
  name: string
  description: string | null
  createdAt: string
  documentCount: number
}

export interface CreateProjectDto {
  name: string
  description?: string
}
