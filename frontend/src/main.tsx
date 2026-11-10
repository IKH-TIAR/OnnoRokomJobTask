import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router'
import App from './App'
import { isRetryable } from './api/errors'
import { ErrorBoundary } from './components/ErrorBoundary'
import { StudentProvider } from './student/StudentContext'
import './index.css'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // Retry only temporary problems (server down / 5xx), never "not found" etc.
      retry: (failureCount, error) => isRetryable(error) && failureCount < 2,
      refetchOnWindowFocus: true,
    },
    mutations: { retry: false },
  },
})

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ErrorBoundary>
      <QueryClientProvider client={queryClient}>
        <StudentProvider>
          <BrowserRouter>
            <App />
          </BrowserRouter>
        </StudentProvider>
      </QueryClientProvider>
    </ErrorBoundary>
  </StrictMode>,
)
