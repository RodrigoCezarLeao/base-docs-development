import { useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { useRegister } from '@/services/auth/actions'

export function RegisterForm() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { mutate: register, isPending, isError, error } = useRegister()

  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    register(
      { name, email, password },
      { onSuccess: () => navigate('/projects') },
    )
  }

  const errorMessage = isError && error instanceof Error ? error.message : null

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      <Input
        label={t('auth.name')}
        type="text"
        value={name}
        onChange={(e) => setName(e.target.value)}
        required
        autoComplete="name"
      />
      <Input
        label={t('auth.email')}
        type="email"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        required
        autoComplete="email"
      />
      <Input
        label={t('auth.password')}
        type="password"
        value={password}
        onChange={(e) => setPassword(e.target.value)}
        required
        autoComplete="new-password"
      />
      {errorMessage && (
        <p className="text-sm text-red-600">{errorMessage}</p>
      )}
      <Button type="submit" loading={isPending} className="w-full">
        {t('auth.register')}
      </Button>
      <p className="text-sm text-center text-gray-600 dark:text-gray-400">
        {t('auth.hasAccount')}{' '}
        <Link to="/login" className="text-blue-600 hover:underline">
          {t('auth.login')}
        </Link>
      </p>
    </form>
  )
}
