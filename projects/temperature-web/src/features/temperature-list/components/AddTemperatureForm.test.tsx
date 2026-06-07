import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { AddTemperatureForm } from './AddTemperatureForm'

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}))

describe('AddTemperatureForm', () => {
  const onSubmit = vi.fn()

  beforeEach(() => onSubmit.mockClear())

  it('renders location input, value input and submit button', () => {
    render(<AddTemperatureForm onSubmit={onSubmit} isLoading={false} />)
    expect(screen.getByPlaceholderText('temperature.location')).toBeInTheDocument()
    expect(screen.getByRole('button')).toBeInTheDocument()
  })

  it('does not call onSubmit when fields are empty', async () => {
    const user = userEvent.setup()
    render(<AddTemperatureForm onSubmit={onSubmit} isLoading={false} />)

    await user.click(screen.getByRole('button'))

    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('calls onSubmit with correct data when form is filled', async () => {
    const user = userEvent.setup()
    render(<AddTemperatureForm onSubmit={onSubmit} isLoading={false} />)

    await user.type(screen.getByPlaceholderText('temperature.location'), 'São Paulo')
    await user.type(screen.getByPlaceholderText(/temperature\.value/), '28.5')
    await user.click(screen.getByRole('button'))

    expect(onSubmit).toHaveBeenCalledWith(
      expect.objectContaining({ location: 'São Paulo', valueCelsius: 28.5 }),
    )
  })

  it('clears fields after successful submit', async () => {
    const user = userEvent.setup()
    render(<AddTemperatureForm onSubmit={onSubmit} isLoading={false} />)

    const locationInput = screen.getByPlaceholderText('temperature.location')
    await user.type(locationInput, 'Recife')
    await user.type(screen.getByPlaceholderText(/temperature\.value/), '32')
    await user.click(screen.getByRole('button'))

    expect(locationInput).toHaveValue('')
  })

  it('disables the button while loading', () => {
    render(<AddTemperatureForm onSubmit={onSubmit} isLoading={true} />)
    expect(screen.getByRole('button')).toBeDisabled()
  })
})
