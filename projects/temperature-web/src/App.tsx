import { QueryClientProvider } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import { Toaster } from 'sonner'
import { ErrorBoundary } from './components/ui/ErrorBoundary'
import './i18n'
import { queryClient } from './lib/queryClient'
import { HomePage } from './pages/home'

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ErrorBoundary>
        <HomePage />
      </ErrorBoundary>
      <Toaster position="bottom-right" richColors />
      {import.meta.env.DEV && <ReactQueryDevtools initialIsOpen={false} />}
    </QueryClientProvider>
  )
}
