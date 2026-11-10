import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import type { ReactElement } from 'react'
import { MemoryRouter, Route, Routes } from 'react-router'
import { StudentProvider } from '../student/StudentContext'

export const STUDENT = '20152691234567890'
export const OTHER_STUDENT = '20159999999999999'

interface Options {
  /** URL to start at, e.g. "/applications/h1". */
  route?: string
  /** Route pattern the element is mounted on, e.g. "/applications/:holdId". */
  path?: string
  /** Student id already "remembered" in localStorage (null = none entered yet). */
  studentId?: string | null
}

/** Renders a page with the same providers as the real app (React Query, router, student). */
export function renderPage(ui: ReactElement, { route = '/', path = '/', studentId = STUDENT }: Options = {}) {
  if (studentId) window.localStorage.setItem('schoolAdmission.studentId', studentId)

  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })

  return render(
    <QueryClientProvider client={queryClient}>
      <StudentProvider>
        <MemoryRouter initialEntries={[route]}>
          <Routes>
            <Route path={path} element={ui} />
            <Route path="*" element={<p>other page</p>} />
          </Routes>
        </MemoryRouter>
      </StudentProvider>
    </QueryClientProvider>,
  )
}

/** ISO timestamp `ms` milliseconds from now. */
export const inMs = (ms: number) => new Date(Date.now() + ms).toISOString()
