import { createBrowserRouter, RouterProvider, Navigate } from 'react-router-dom'
import { lazy } from 'react'

const LoginPage = lazy(() => import('@/pages/login'))
const RegisterPage = lazy(() => import('@/pages/register'))
const ProjectsPage = lazy(() => import('@/pages/projects'))
const CanvasPage = lazy(() => import('@/pages/canvas'))

const router = createBrowserRouter([
  { path: '/', element: <Navigate to="/projects" replace /> },
  { path: '/login', element: <LoginPage /> },
  { path: '/register', element: <RegisterPage /> },
  { path: '/projects', element: <ProjectsPage /> },
  { path: '/canvas/:projectId', element: <CanvasPage /> },
])

export function Router() {
  return <RouterProvider router={router} />
}
