export interface AccessEventDto {
  id: number
  userId: number | null
  ip: string
  browser: string
  os: string
  deviceType: string
  method: string
  path: string
  statusCode: number
  country: string | null
  city: string | null
  occurredAt: string
}

export interface AccessFilters {
  userId?: number
  from?: string
  to?: string
  q?: string
  page?: number
  pageSize?: number
}
