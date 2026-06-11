import { PersistQueryClientProvider } from '@tanstack/react-query-persist-client'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import { Suspense } from 'react'
import { persistOptions } from '@/lib/cache'
import { queryClient } from '@/lib/queryClient'
import '@/i18n'
import '@/styles/global.css'
import { Router } from '@/router'
import { VersionBadge } from '@/components/ui/VersionBadge'

export default function App() {
  return (
    <PersistQueryClientProvider client={queryClient} persistOptions={persistOptions}>
      <Suspense fallback={<div>Loading...</div>}>
        <Router />
      </Suspense>
      <VersionBadge />
      {import.meta.env.DEV && <ReactQueryDevtools />}
    </PersistQueryClientProvider>
  )
}
