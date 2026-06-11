import { PersistQueryClientProvider } from '@tanstack/react-query-persist-client'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import { Suspense } from 'react'
import { Toaster } from 'sonner'
import { ErrorBoundary } from './components/ui/ErrorBoundary'
import { SettingsMenu } from './components/ui/SettingsMenu'
import { VersionBadge } from './components/ui/VersionBadge'
import './i18n'
import { persistOptions } from './lib/cache'
import { queryClient } from './lib/queryClient'
import { Router } from './router'

export function App() {
  return (
    <PersistQueryClientProvider client={queryClient} persistOptions={persistOptions}>
      <ErrorBoundary>
        <Suspense fallback={null}>
          <Router />
        </Suspense>
      </ErrorBoundary>
      <Toaster position="bottom-right" richColors />
      <SettingsMenu />
      <VersionBadge />
      {import.meta.env.DEV && <ReactQueryDevtools initialIsOpen={false} />}
    </PersistQueryClientProvider>
  )
}
