import { createBrowserRouter, RouterProvider } from 'react-router-dom'
import { lazy } from 'react'
import { HomePage } from '@/pages/home'

const LoginPage = lazy(() => import('@/pages/login'))
const RegisterPage = lazy(() => import('@/pages/register'))
const AdminLogsPage = lazy(() => import('@/pages/admin-logs'))
const AdminMetricsPage = lazy(() => import('@/pages/admin-metrics'))
const AdminAccessPage = lazy(() => import('@/pages/admin-access'))
const PrivacyPage = lazy(() => import('@/pages/privacy'))

const router = createBrowserRouter([
  { path: '/', element: <HomePage /> },
  { path: '/login', element: <LoginPage /> },
  { path: '/register', element: <RegisterPage /> },
  { path: '/privacy', element: <PrivacyPage /> },
  { path: '/admin/logs', element: <AdminLogsPage /> },
  { path: '/admin/metrics', element: <AdminMetricsPage /> },
  { path: '/admin/access', element: <AdminAccessPage /> },
])

export function Router() {
  return <RouterProvider router={router} />
}
