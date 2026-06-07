import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { createElement, type ReactNode } from 'react'
import { useProjects } from './queries'
import { api } from '@/lib/api'

vi.mock('@/lib/api', () => ({
  api: { get: vi.fn() },
}))

const mockGet = vi.mocked(api.get)

function wrapper({ children }: { children: ReactNode }) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })
  return createElement(QueryClientProvider, { client }, children)
}

describe('useProjects', () => {
  beforeEach(() => mockGet.mockReset())

  it('fetches and exposes the projects list', async () => {
    const projects = [
      { id: 1, name: 'My Docs', description: null, createdAt: '2024-01-01', documentCount: 3 },
    ]
    mockGet.mockResolvedValueOnce({ success: true, data: projects })

    const { result } = renderHook(() => useProjects(), { wrapper })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(mockGet).toHaveBeenCalledWith('/api/v1/projects')
    expect(result.current.data).toEqual(projects)
  })

  it('returns empty array when data is null', async () => {
    mockGet.mockResolvedValueOnce({ success: true, data: null })

    const { result } = renderHook(() => useProjects(), { wrapper })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(result.current.data).toEqual([])
  })
})
