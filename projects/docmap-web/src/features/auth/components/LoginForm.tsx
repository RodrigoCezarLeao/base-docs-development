import { useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { useLogin } from '@/services/auth/actions'

export function LoginForm() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { mutate: login, isPending, isError } = useLogin()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    login(
      { email, password },
      { onSuccess: () => navigate('/projects') },
    )
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
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
        autoComplete="current-password"
      />
      {isError && (
        <p className="text-sm text-red-600">{t('auth.loginError')}</p>
      )}
      <Button type="submit" loading={isPending} className="w-full">
        {t('auth.login')}
      </Button>
      <p className="text-sm text-center text-gray-600">
        {t('auth.noAccount')}{' '}
        <Link to="/register" className="text-blue-600 hover:underline">
          {t('auth.register')}
        </Link>
      </p>
    </form>
  )
}
