import { describe, it, expect, vi, beforeEach } from 'vitest'
import { ping, checkHealth } from './index'
import { api } from '@/lib/api'

vi.mock('@/lib/api', () => ({
  api: { get: vi.fn() },
}))

const mockGet = vi.mocked(api.get)

describe('ping', () => {
  beforeEach(() => mockGet.mockReset())

  it('calls GET /ping and returns status ok', async () => {
    mockGet.mockResolvedValue({ status: 'ok' })

    const result = await ping()

    expect(mockGet).toHaveBeenCalledWith('/ping')
    expect(result).toEqual({ status: 'ok' })
  })

  it('rejects when the api call fails', async () => {
    mockGet.mockRejectedValueOnce(new Error('Network Error'))

    await expect(ping()).rejects.toThrow('Network Error')
  })
})

describe('checkHealth', () => {
  beforeEach(() => mockGet.mockReset())

  it('calls GET /health and returns Healthy status', async () => {
    mockGet.mockResolvedValue({ status: 'Healthy', checks: [] })

    const result = await checkHealth()

    expect(mockGet).toHaveBeenCalledWith('/health')
    expect(result.status).toBe('Healthy')
  })

  it('returns Unhealthy status with check details when a dependency fails', async () => {
    mockGet.mockResolvedValue({
      status: 'Unhealthy',
      checks: [{ name: 'postgresql', status: 'Unhealthy', description: 'Connection refused' }],
    })

    const result = await checkHealth()

    expect(result.status).toBe('Unhealthy')
    expect(result.checks[0]).toMatchObject({ name: 'postgresql', status: 'Unhealthy' })
  })
})
