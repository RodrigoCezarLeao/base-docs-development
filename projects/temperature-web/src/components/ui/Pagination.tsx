import { Button } from './Button'

interface PaginationProps {
  page: number
  totalPages: number
  onPageChange: (page: number) => void
}

export function Pagination({ page, totalPages, onPageChange }: PaginationProps) {
  if (totalPages <= 1) return null

  return (
    <nav aria-label="Paginação" className="flex items-center justify-center gap-2 mt-6">
      <Button variant="secondary" disabled={page <= 1} onClick={() => onPageChange(page - 1)}>
        ←
      </Button>
      <span className="min-w-[5rem] text-center text-sm text-gray-700">
        {page} / {totalPages}
      </span>
      <Button variant="secondary" disabled={page >= totalPages} onClick={() => onPageChange(page + 1)}>
        →
      </Button>
    </nav>
  )
}
