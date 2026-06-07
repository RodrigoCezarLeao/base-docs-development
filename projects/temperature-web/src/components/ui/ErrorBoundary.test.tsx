import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { ErrorBoundary } from './ErrorBoundary'

function Bomb({ explode }: { explode: boolean }) {
  if (explode) throw new Error('boom')
  return <p>ok</p>
}

describe('ErrorBoundary', () => {
  it('renders children when there is no error', () => {
    render(
      <ErrorBoundary>
        <Bomb explode={false} />
      </ErrorBoundary>,
    )
    expect(screen.getByText('ok')).toBeInTheDocument()
  })

  it('renders default fallback when a child throws', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})

    render(
      <ErrorBoundary>
        <Bomb explode={true} />
      </ErrorBoundary>,
    )

    expect(screen.getByText('Algo deu errado.')).toBeInTheDocument()
    consoleError.mockRestore()
  })

  it('renders custom fallback when provided', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})

    render(
      <ErrorBoundary fallback={<p>Erro customizado</p>}>
        <Bomb explode={true} />
      </ErrorBoundary>,
    )

    expect(screen.getByText('Erro customizado')).toBeInTheDocument()
    consoleError.mockRestore()
  })

  it('recovers when "Tentar novamente" is clicked', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})

    const { rerender } = render(
      <ErrorBoundary>
        <Bomb explode={true} />
      </ErrorBoundary>,
    )

    // Atualiza o filho para não explodir antes de limpar o estado de erro,
    // caso contrário o reset causa um novo throw imediato.
    rerender(
      <ErrorBoundary>
        <Bomb explode={false} />
      </ErrorBoundary>,
    )

    await userEvent.click(screen.getByText('Tentar novamente'))

    expect(screen.getByText('ok')).toBeInTheDocument()
    consoleError.mockRestore()
  })
})
