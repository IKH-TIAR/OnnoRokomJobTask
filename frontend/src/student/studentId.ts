import { createContext, useContext } from 'react'

export const STUDENT_ID_LENGTH = 17

export function isValidStudentId(value: string): boolean {
  return new RegExp(`^\\d{${STUDENT_ID_LENGTH}}$`).test(value)
}

export interface StudentContextValue {
  /** The current student's 17-digit birth registration number, or null if not entered yet. */
  studentId: string | null
  setStudentId: (value: string | null) => void
}

export const StudentContext = createContext<StudentContextValue | null>(null)

export function useStudent(): StudentContextValue {
  const context = useContext(StudentContext)
  if (!context) throw new Error('useStudent must be used inside <StudentProvider>')
  return context
}
