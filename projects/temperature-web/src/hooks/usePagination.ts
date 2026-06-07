import { useCallback, useState } from 'react'

interface UsePaginationOptions {
  initialPage?: number
  pageSize?: number
}

export function usePagination({ initialPage = 1, pageSize = 10 }: UsePaginationOptions = {}) {
  const [page, setPage] = useState(initialPage)

  const nextPage = useCallback(() => setPage((p) => p + 1), [])
  const prevPage = useCallback(() => setPage((p) => Math.max(1, p - 1)), [])
  const goToPage = useCallback((n: number) => setPage(n), [])
  const reset = useCallback(() => setPage(initialPage), [initialPage])

  return { page, pageSize, setPage, nextPage, prevPage, goToPage, reset }
}
