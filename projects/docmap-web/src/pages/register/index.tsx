import { Navigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useAuth } from '@/stores/auth/hooks'
import { RegisterForm } from '@/features/auth'

export default function RegisterPage() {
  const { t } = useTranslation()
  const { isAuthenticated } = useAuth()

  if (isAuthenticated) {
    return <Navigate to="/projects" replace />
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <div className="bg-white rounded-xl shadow-md p-8 w-full max-w-sm">
        <h1 className="text-2xl font-bold text-gray-900 mb-6 text-center">
          {t('auth.registerTitle')}
        </h1>
        <RegisterForm />
      </div>
    </div>
  )
}
