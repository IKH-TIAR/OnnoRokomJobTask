import { useState, type FormEvent } from 'react'
import { STUDENT_ID_LENGTH, isValidStudentId, useStudent } from '../student/studentId'

/** The box at the top of every page where the student's birth registration number is entered. */
export function StudentIdBar() {
  const { studentId, setStudentId } = useStudent()
  const [editing, setEditing] = useState(studentId === null)
  const [draft, setDraft] = useState(studentId ?? '')
  const [error, setError] = useState<string | null>(null)

  function submit(event: FormEvent) {
    event.preventDefault()
    const value = draft.trim()
    if (!isValidStudentId(value)) {
      setError(`Please enter exactly ${STUDENT_ID_LENGTH} digits (numbers only).`)
      return
    }
    setError(null)
    setStudentId(value)
    setEditing(false)
  }

  if (!editing && studentId) {
    return (
      <div className="flex flex-wrap items-center gap-3 text-sm">
        <span className="text-slate-600">Student (birth reg. no.):</span>
        <span className="rounded bg-slate-100 px-2 py-1 font-mono font-semibold tracking-wider">{studentId}</span>
        <button
          type="button"
          onClick={() => {
            setDraft(studentId)
            setEditing(true)
          }}
          className="text-indigo-600 hover:underline"
        >
          Change
        </button>
      </div>
    )
  }

  return (
    <form onSubmit={submit} className="flex flex-col gap-1" noValidate>
      <div className="flex flex-wrap items-center gap-2">
        <label htmlFor="student-id" className="text-sm text-slate-600">
          Student’s birth registration number:
        </label>
        <input
          id="student-id"
          inputMode="numeric"
          autoComplete="off"
          maxLength={STUDENT_ID_LENGTH}
          placeholder={'e.g. ' + '2015'.padEnd(STUDENT_ID_LENGTH, '0')}
          value={draft}
          onChange={(e) => {
            setDraft(e.target.value.replace(/\D/g, ''))
            setError(null)
          }}
          aria-invalid={error !== null}
          aria-describedby="student-id-help"
          className="w-56 rounded-md border border-slate-300 px-3 py-1.5 font-mono tracking-wider focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200"
        />
        <button type="submit" className="rounded-md bg-indigo-600 px-3 py-1.5 text-sm font-semibold text-white hover:bg-indigo-700">
          Continue
        </button>
        {studentId && (
          <button type="button" onClick={() => setEditing(false)} className="text-sm text-slate-500 hover:underline">
            Cancel
          </button>
        )}
      </div>
      <p id="student-id-help" className={`text-xs ${error ? 'text-red-600' : 'text-slate-500'}`}>
        {error ?? `${STUDENT_ID_LENGTH} digits · ${draft.length}/${STUDENT_ID_LENGTH}`}
      </p>
    </form>
  )
}
