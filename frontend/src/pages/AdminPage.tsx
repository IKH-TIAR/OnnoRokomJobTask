import { useState, type FormEvent, type ReactNode } from 'react'
import { Link } from 'react-router'
import { ApiError } from '../api/client'
import { friendlyMessage } from '../api/errors'
import { useCreateClass } from '../api/queries'
import { Alert } from '../components/Alert'

const MAX_SEATS = 10_000
const MAX_NAME = 200

type FieldErrors = Partial<Record<'name' | 'totalSeats', string>>

function validate(name: string, seats: string): FieldErrors {
  const errors: FieldErrors = {}
  if (!name.trim()) errors.name = 'Please enter a class name.'
  else if (name.trim().length > MAX_NAME) errors.name = `Class name must be at most ${MAX_NAME} characters.`

  const n = Number(seats)
  if (!seats.trim()) errors.totalSeats = 'Please enter the number of seats.'
  else if (!Number.isInteger(n) || n < 1 || n > MAX_SEATS)
    errors.totalSeats = `Seats must be a whole number between 1 and ${MAX_SEATS}.`
  return errors
}

/** School admin: create a class (each shift is its own class). */
export function AdminPage() {
  const createClass = useCreateClass()
  const [name, setName] = useState('')
  const [seats, setSeats] = useState('')
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [created, setCreated] = useState<string | null>(null)

  function submit(event: FormEvent) {
    event.preventDefault()
    setCreated(null)

    const errors = validate(name, seats)
    setFieldErrors(errors)
    if (Object.keys(errors).length > 0) return

    createClass.mutate(
      { name: name.trim(), totalSeats: Number(seats) },
      {
        onSuccess: (c) => {
          setCreated(`Created “${c.name}” with ${c.totalSeats} seats.`)
          setName('')
          setSeats('')
        },
        onError: (error) => {
          // Show the server's per-field messages next to the right inputs
          if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
            setFieldErrors({
              name: error.fieldErrors.name?.[0],
              totalSeats: error.fieldErrors.totalSeats?.[0],
            })
          }
        },
      },
    )
  }

  return (
    <section className="flex max-w-lg flex-col gap-4">
      <div>
        <h2 className="text-xl font-bold">Create a class</h2>
        <p className="text-sm text-slate-500">
          Each shift is a separate class with its own seats, e.g. “Class 6 - Morning Shift” and “Class 6 - Day Shift”.
        </p>
      </div>

      {created && (
        <Alert kind="success" onClose={() => setCreated(null)}>
          {created}{' '}
          <Link to="/" className="font-semibold underline">
            View classes
          </Link>
        </Alert>
      )}

      {createClass.isError && !(createClass.error instanceof ApiError && createClass.error.code === 'VALIDATION_FAILED') && (
        <Alert kind="error">{friendlyMessage(createClass.error)}</Alert>
      )}

      <form onSubmit={submit} noValidate className="flex flex-col gap-4 rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
        <Field label="Class name" id="class-name" error={fieldErrors.name}>
          <input
            id="class-name"
            value={name}
            maxLength={MAX_NAME}
            onChange={(e) => setName(e.target.value)}
            placeholder="Class 6 - Morning Shift"
            aria-invalid={!!fieldErrors.name}
            className={inputClass(!!fieldErrors.name)}
          />
        </Field>

        <Field label="Number of seats" id="class-seats" error={fieldErrors.totalSeats}>
          <input
            id="class-seats"
            type="number"
            inputMode="numeric"
            min={1}
            max={MAX_SEATS}
            value={seats}
            onChange={(e) => setSeats(e.target.value)}
            placeholder="120"
            aria-invalid={!!fieldErrors.totalSeats}
            className={inputClass(!!fieldErrors.totalSeats)}
          />
        </Field>

        <button
          type="submit"
          disabled={createClass.isPending}
          className="rounded-md bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:bg-slate-300"
        >
          {createClass.isPending ? 'Creating…' : 'Create class'}
        </button>
      </form>
    </section>
  )
}

function inputClass(invalid: boolean) {
  return `w-full rounded-md border px-3 py-2 focus:outline-none focus:ring-2 ${
    invalid ? 'border-red-400 focus:ring-red-200' : 'border-slate-300 focus:border-indigo-500 focus:ring-indigo-200'
  }`
}

function Field({ label, id, error, children }: { label: string; id: string; error?: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-1">
      <label htmlFor={id} className="text-sm font-medium">
        {label}
      </label>
      {children}
      {error && <p className="text-xs text-red-600">{error}</p>}
    </div>
  )
}
