import { useCallback, useMemo, useState, type ReactNode } from 'react'
import { StudentContext, isValidStudentId } from './studentId'

const STORAGE_KEY = 'schoolAdmission.studentId'

// localStorage can throw (private mode, blocked storage); the app must still work without it.
function readStored(): string | null {
  try {
    const value = window.localStorage.getItem(STORAGE_KEY)
    return value && isValidStudentId(value) ? value : null
  } catch {
    return null
  }
}

function writeStored(value: string | null) {
  try {
    if (value) window.localStorage.setItem(STORAGE_KEY, value)
    else window.localStorage.removeItem(STORAGE_KEY)
  } catch {
    // ignore
  }
}

/** Remembers the student id across page refreshes and browser restarts. */
export function StudentProvider({ children }: { children: ReactNode }) {
  const [studentId, setState] = useState<string | null>(readStored)

  const setStudentId = useCallback((value: string | null) => {
    setState(value)
    writeStored(value)
  }, [])

  const value = useMemo(() => ({ studentId, setStudentId }), [studentId, setStudentId])
  return <StudentContext.Provider value={value}>{children}</StudentContext.Provider>
}
