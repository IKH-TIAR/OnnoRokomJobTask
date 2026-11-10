import { useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { ApiError } from '../api/client'
import { friendlyMessage } from '../api/errors'
import { useApplyForSeat, useClasses } from '../api/queries'
import type { ClassSeats } from '../api/types'
import { Alert } from '../components/Alert'
import { SeatBar } from '../components/SeatBar'
import { POLL_INTERVAL_MS } from '../config'
import { useStudent } from '../student/studentId'

export function ClassesPage() {
  const { studentId } = useStudent()
  const classes = useClasses()
  const apply = useApplyForSeat(studentId)
  const navigate = useNavigate()
  const [applyError, setApplyError] = useState<unknown>(null)

  function onApply(schoolClass: ClassSeats) {
    setApplyError(null)
    apply.mutate(schoolClass.id, {
      onSuccess: (hold) => navigate(`/applications/${hold.holdId}`),
      onError: (error) => setApplyError(error),
    })
  }

  return (
    <section className="flex flex-col gap-4">
      <div className="flex flex-wrap items-end justify-between gap-2">
        <div>
          <h2 className="text-xl font-bold">Classes</h2>
          <p className="text-sm text-slate-500">
            Seats are given first come, first served. Seat numbers update every {POLL_INTERVAL_MS / 1000} seconds.
          </p>
        </div>
        <Legend />
      </div>

      {!studentId && (
        <Alert kind="info">Enter the student’s birth registration number at the top of the page to apply.</Alert>
      )}

      {applyError !== null && (
        <Alert kind="error" onClose={() => setApplyError(null)}>
          {friendlyMessage(applyError)}{' '}
          {applyError instanceof ApiError && applyError.code === 'ALREADY_APPLIED' && (
            <Link to="/my-applications" className="font-semibold underline">
              Open my applications
            </Link>
          )}
        </Alert>
      )}

      {/* Refresh failed but we still have data: keep showing it, with a warning. */}
      {classes.isError && classes.data && (
        <Alert kind="warning">
          Lost connection to the server. Showing the last known seat numbers; retrying automatically…
        </Alert>
      )}

      {classes.isPending ? (
        <p className="text-slate-500">Loading classes…</p>
      ) : classes.isError && !classes.data ? (
        <Alert kind="error">{friendlyMessage(classes.error)} Retrying automatically…</Alert>
      ) : classes.data.length === 0 ? (
        <div className="rounded-xl border border-dashed border-slate-300 bg-white p-8 text-center text-slate-500">
          No classes yet.{' '}
          <Link to="/admin" className="font-semibold text-indigo-600 hover:underline">
            Create one in Admin
          </Link>
          .
        </div>
      ) : (
        <ul className="grid gap-3 sm:grid-cols-2">
          {classes.data.map((c) => (
            <ClassCard
              key={c.id}
              schoolClass={c}
              canApply={studentId !== null}
              applying={apply.isPending && apply.variables === c.id}
              disabled={apply.isPending}
              onApply={() => onApply(c)}
            />
          ))}
        </ul>
      )}
    </section>
  )
}

interface ClassCardProps {
  schoolClass: ClassSeats
  canApply: boolean
  applying: boolean
  disabled: boolean
  onApply: () => void
}

function ClassCard({ schoolClass: c, canApply, applying, disabled, onApply }: ClassCardProps) {
  const full = c.availableSeats <= 0

  return (
    <li className="flex flex-col gap-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
      <div className="flex items-start justify-between gap-2">
        <h3 className="font-semibold">{c.name}</h3>
        <span
          className={`shrink-0 rounded-full px-2 py-0.5 text-xs font-semibold ${
            full ? 'bg-red-100 text-red-700' : 'bg-green-100 text-green-700'
          }`}
        >
          {full ? 'Full' : `${c.availableSeats} free`}
        </span>
      </div>

      <SeatBar seats={c} />

      <dl className="grid grid-cols-4 gap-2 text-center text-xs">
        <Stat label="Total" value={c.totalSeats} />
        <Stat label="On hold" value={c.heldSeats} className="text-amber-600" />
        <Stat label="Confirmed" value={c.confirmedSeats} className="text-green-600" />
        <Stat label="Free" value={c.availableSeats} className="text-slate-900" />
      </dl>

      <button
        type="button"
        onClick={onApply}
        disabled={!canApply || full || disabled}
        title={!canApply ? 'Enter the birth registration number first' : full ? 'No seats left' : undefined}
        className="rounded-md bg-indigo-600 px-3 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:cursor-not-allowed disabled:bg-slate-300"
      >
        {applying ? 'Applying…' : full ? 'No seats left' : 'Apply'}
      </button>
    </li>
  )
}

function Stat({ label, value, className = '' }: { label: string; value: number; className?: string }) {
  return (
    <div>
      <dt className="text-slate-500">{label}</dt>
      <dd className={`text-base font-bold ${className}`}>{value}</dd>
    </div>
  )
}

function Legend() {
  return (
    <div className="flex gap-3 text-xs text-slate-500">
      <span className="flex items-center gap-1">
        <span className="h-2 w-2 rounded-full bg-green-500" /> Confirmed
      </span>
      <span className="flex items-center gap-1">
        <span className="h-2 w-2 rounded-full bg-amber-400" /> On hold
      </span>
      <span className="flex items-center gap-1">
        <span className="h-2 w-2 rounded-full bg-slate-200" /> Free
      </span>
    </div>
  )
}
