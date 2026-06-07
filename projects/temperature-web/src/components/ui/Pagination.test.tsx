import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { Pagination } from './Pagination'

describe('Pagination', () => {
  it('renders page indicator', () => {
    render(<Pagination page={2} totalPages={5} onPageChange={vi.fn()} />)
    expect(screen.getByText('2 / 5')).toBeInTheDocument()
  })

  it('renders nothing when totalPages is 1', () => {
    const { container } = render(<Pagination page={1} totalPages={1} onPageChange={vi.fn()} />)
    expect(container.firstChild).toBeNull()
  })

  it('disables prev button on first page', () => {
    render(<Pagination page={1} totalPages={5} onPageChange={vi.fn()} />)
    expect(screen.getByText('←').closest('button')).toBeDisabled()
  })

  it('disables next button on last page', () => {
    render(<Pagination page={5} totalPages={5} onPageChange={vi.fn()} />)
    expect(screen.getByText('→').closest('button')).toBeDisabled()
  })

  it('calls onPageChange with page - 1 when prev is clicked', async () => {
    const onChange = vi.fn()
    render(<Pagination page={3} totalPages={5} onPageChange={onChange} />)
    await userEvent.click(screen.getByText('←'))
    expect(onChange).toHaveBeenCalledWith(2)
  })

  it('calls onPageChange with page + 1 when next is clicked', async () => {
    const onChange = vi.fn()
    render(<Pagination page={3} totalPages={5} onPageChange={onChange} />)
    await userEvent.click(screen.getByText('→'))
    expect(onChange).toHaveBeenCalledWith(4)
  })
})
