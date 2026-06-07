export interface ApiResponse<T> {
  success: boolean
  data: T
  message: string | null
  errors: string[] | null
}

export interface PagedResponse<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}
