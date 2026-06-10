import { QueryClientProvider } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import { Suspense } from 'react'
import { Toaster } from 'sonner'
import { ErrorBoundary } from './components/ui/ErrorBoundary'
import { VersionBadge } from './components/ui/VersionBadge'
import './i18n'
import { queryClient } from './lib/queryClient'
import { Router } from './router'

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ErrorBoundary>
        <Suspense fallback={null}>
          <Router />
        </Suspense>
      </ErrorBoundary>
      <Toaster position="bottom-right" richColors />
      <VersionBadge />
      {import.meta.env.DEV && <ReactQueryDevtools initialIsOpen={false} />}
    </QueryClientProvider>
  )
}
