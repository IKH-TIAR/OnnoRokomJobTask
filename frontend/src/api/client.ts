import { apiBaseUrl } from '../config'
import type { ClassSeats, CreateClassInput, Hold, SchoolClass } from './types'

const STUDENT_HEADER = 'X-Student-Id'

/** Every failed API call (including "server unreachable") becomes an ApiError. */
export class ApiError extends Error {
  /** HTTP status; 0 when the server could not be reached. */
  readonly status: number
  /** Backend error code (e.g. CLASS_FULL) or NETWORK_ERROR / HTTP_<status>. */
  readonly code: string
  /** Per-field validation messages, keyed by field name (e.g. totalSeats). */
  readonly fieldErrors: Record<string, string[]>

  constructor(status: number, code: string, message: string, fieldErrors: Record<string, string[]> = {}) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
    this.fieldErrors = fieldErrors
  }

  get isNetworkError() {
    return this.status === 0
  }
}

interface ProblemBody {
  code?: string
  detail?: string
  title?: string
  errors?: Record<string, string[]>
}

interface RequestOptions {
  method?: 'GET' | 'POST' | 'DELETE'
  body?: unknown
  studentId?: string
}

async function request<T>(path: string, { method = 'GET', body, studentId }: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' }
  if (body !== undefined) headers['Content-Type'] = 'application/json'
  if (studentId) headers[STUDENT_HEADER] = studentId

  let response: Response
  try {
    response = await fetch(`${apiBaseUrl}${path}`, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
    })
  } catch {
    throw new ApiError(0, 'NETWORK_ERROR', 'Cannot reach the server.')
  }

  if (response.status === 204) return undefined as T

  const text = await response.text()
  let data: unknown
  try {
    data = text ? JSON.parse(text) : undefined
  } catch {
    data = undefined
  }

  if (!response.ok) {
    const problem = (data ?? {}) as ProblemBody
    throw new ApiError(
      response.status,
      problem.code ?? `HTTP_${response.status}`,
      problem.detail ?? problem.title ?? `Request failed (${response.status}).`,
      problem.errors ?? {},
    )
  }

  return data as T
}

export const api = {
  listClasses: () => request<ClassSeats[]>('/classes'),
  getClass: (classId: string) => request<ClassSeats>(`/classes/${classId}`),
  createClass: (input: CreateClassInput) => request<SchoolClass>('/classes', { method: 'POST', body: input }),

  applyForSeat: (classId: string, studentId: string) =>
    request<Hold>(`/classes/${classId}/holds`, { method: 'POST', studentId }),
  getHold: (holdId: string) => request<Hold>(`/holds/${holdId}`),
  confirmHold: (holdId: string, studentId: string) =>
    request<Hold>(`/holds/${holdId}/confirm`, { method: 'POST', studentId }),
  cancelHold: (holdId: string, studentId: string) =>
    request<void>(`/holds/${holdId}`, { method: 'DELETE', studentId }),
  myHolds: (studentId: string) => request<Hold[]>('/me/holds', { studentId }),
}
