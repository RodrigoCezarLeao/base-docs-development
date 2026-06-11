import { createBrowserRouter, RouterProvider } from 'react-router-dom'
import { lazy } from 'react'
import { HomePage } from '@/pages/home'

const LoginPage = lazy(() => import('@/pages/login'))
const RegisterPage = lazy(() => import('@/pages/register'))
const AdminLogsPage = lazy(() => import('@/pages/admin-logs'))
const AdminMetricsPage = lazy(() => import('@/pages/admin-metrics'))

const router = createBrowserRouter([
  { path: '/', element: <HomePage /> },
  { path: '/login', element: <LoginPage /> },
  { path: '/register', element: <RegisterPage /> },
  { path: '/admin/logs', element: <AdminLogsPage /> },
  { path: '/admin/metrics', element: <AdminMetricsPage /> },
])

export function Router() {
  return <RouterProvider router={router} />
}
