import { QueryClientProvider } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import { Suspense } from 'react'
import { queryClient } from '@/lib/queryClient'
import '@/i18n'
import '@/styles/global.css'
import { Router } from '@/router'

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <Suspense fallback={<div>Loading...</div>}>
        <Router />
      </Suspense>
      <ReactQueryDevtools />
    </QueryClientProvider>
  )
}
