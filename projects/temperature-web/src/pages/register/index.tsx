import { useState } from 'react'
import { Navigate, Link, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useAuth } from '@/stores/auth/hooks'
import { useRegister } from '@/services/auth/actions'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'

export default function RegisterPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { isAuthenticated } = useAuth()
  const { mutate: register, isPending, isError } = useRegister()
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')

  if (isAuthenticated) return <Navigate to="/" replace />

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    register({ name, email, password }, { onSuccess: () => navigate('/') })
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900">
      <form onSubmit={handleSubmit} className="bg-white dark:bg-gray-800 rounded-xl shadow-md p-8 w-full max-w-sm flex flex-col gap-4">
        <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100 text-center">{t('auth.registerTitle')}</h1>
        <Input label={t('auth.name')} value={name} onChange={(e) => setName(e.target.value)} required />
        <Input label={t('auth.email')} type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
        <Input label={t('auth.password')} type="password" value={password} onChange={(e) => setPassword(e.target.value)} required minLength={6} />
        {isError && <p className="text-sm text-red-600">{t('common.error')}</p>}
        <Button type="submit" loading={isPending}>{t('auth.register')}</Button>
        <p className="text-sm text-center text-gray-600 dark:text-gray-400">
          {t('auth.hasAccount')}{' '}
          <Link to="/login" className="text-blue-600 hover:underline">{t('auth.login')}</Link>
        </p>
      </form>
    </div>
  )
}
