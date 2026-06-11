import { Navigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useAuth } from '@/stores/auth/hooks'
import { LoginForm } from '@/features/auth'

export default function LoginPage() {
  const { t } = useTranslation()
  const { isAuthenticated } = useAuth()

  if (isAuthenticated) {
    return <Navigate to="/projects" replace />
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900">
      <div className="bg-white dark:bg-gray-800 rounded-xl shadow-md p-8 w-full max-w-sm">
        <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100 mb-6 text-center">
          {t('auth.loginTitle')}
        </h1>
        <LoginForm />
      </div>
    </div>
  )
}
