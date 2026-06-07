import { render, screen } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import { Button } from './Button'

describe('Button', () => {
  it('renders children', () => {
    render(<Button>Salvar</Button>)
    expect(screen.getByRole('button', { name: 'Salvar' })).toBeInTheDocument()
  })

  it('shows "..." when loading', () => {
    render(<Button loading>Salvar</Button>)
    expect(screen.getByRole('button')).toHaveTextContent('...')
  })

  it('is disabled when loading', () => {
    render(<Button loading>Salvar</Button>)
    expect(screen.getByRole('button')).toBeDisabled()
  })

  it('is disabled when disabled prop is set', () => {
    render(<Button disabled>Salvar</Button>)
    expect(screen.getByRole('button')).toBeDisabled()
  })

  it('applies variant class correctly', () => {
    render(<Button variant="danger">Excluir</Button>)
    expect(screen.getByRole('button')).toHaveClass('bg-red-600')
  })
})
