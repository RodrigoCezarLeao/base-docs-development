import { createBrowserRouter, RouterProvider, Navigate } from 'react-router-dom'
import { lazy } from 'react'

const LoginPage = lazy(() => import('@/pages/login'))
const RegisterPage = lazy(() => import('@/pages/register'))
const ProjectsPage = lazy(() => import('@/pages/projects'))
const CanvasPage = lazy(() => import('@/pages/canvas'))
const AdminLogsPage = lazy(() => import('@/pages/admin-logs'))
const AdminMetricsPage = lazy(() => import('@/pages/admin-metrics'))

const router = createBrowserRouter([
  { path: '/', element: <Navigate to="/projects" replace /> },
  { path: '/login', element: <LoginPage /> },
  { path: '/register', element: <RegisterPage /> },
  { path: '/projects', element: <ProjectsPage /> },
  { path: '/canvas/:projectId', element: <CanvasPage /> },
  { path: '/admin/logs', element: <AdminLogsPage /> },
  { path: '/admin/metrics', element: <AdminMetricsPage /> },
])

export function Router() {
  return <RouterProvider router={router} />
}
