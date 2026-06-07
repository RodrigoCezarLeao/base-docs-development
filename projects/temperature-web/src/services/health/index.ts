import { api } from '@/lib/api'

export interface HealthCheckResult {
  status: string
  checks: Array<{
    name: string
    status: string
    description: string | null
  }>
}

export async function ping(): Promise<{ status: string }> {
  return api.get('/ping')
}

export async function checkHealth(): Promise<HealthCheckResult> {
  return api.get('/health')
}
