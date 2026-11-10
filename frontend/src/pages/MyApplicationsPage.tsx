import { useQueryClient } from '@tanstack/react-query'
import { useEffect } from 'react'
import { Link } from 'react-router'
import { friendlyMessage } from '../api/errors'
import { queryKeys, useMyHolds } from '../api/queries'
import type { Hold } from '../api/types'
import { Alert } from '../components/Alert'
import { Countdown } from '../components/Countdown'
import { HoldActions } from '../components/HoldActions'
import { StatusBadge } from '../components/StatusBadge'
import { displayStatus, formatDateTime, msRemaining, useNow } from '../holds/holdTime'
import { useStudent } from '../student/studentId'

/** Every application of the current student and its status. */
export function MyApplicationsPage() {
  const { studentId } = useStudent()
  const holds = useMyHolds(studentId)
  const queryClient = useQueryClient()
  const now = useNow()

  // When any countdown hits zero, refresh from the server (and seat counts).
  const anyExpiredByClock = holds.data?.some((h) => h.status === 'HELD' && displayStatus(h, now) === 'EXPIRED') ?? false
  useEffect(() => {
    if (!anyExpiredByClock || !studentId) return
    queryClient.invalidateQueries({ queryKey: queryKeys.myHolds(studentId) })
    queryClient.invalidateQueries({ queryKey: queryKeys.classes })
  }, [anyExpiredByClock, studentId, queryClient])

  if (!studentId) {
    return (
      <section className="flex flex-col gap-3">
        <h2 className="text-xl font-bold">My applications</h2>
        <Alert kind="info">Enter the student’s birth registration number at the top of the page to see their applications.</Alert>
      </section>
    )
  }

  return (
    <section className="flex flex-col gap-4">
      <div>
        <h2 className="text-xl font-bold">My applications</h2>
        <p className="text-sm text-slate-500">
          All applications for <span className="font-mono">{studentId}</span>, newest first.
        </p>
      </div>

      {holds.isError && holds.data && (
        <Alert kind="warning">Lost connection to the server. Showing the last known status; retrying automatically…</Alert>
      )}

      {holds.isPending ? (
        <p className="text-slate-500">Loading applications…</p>
      ) : holds.isError && !holds.data ? (
        <Alert kind="error">{friendlyMessage(holds.error)} Retrying automatically…</Alert>
      ) : holds.data.length === 0 ? (
        <div className="rounded-xl border border-dashed border-slate-300 bg-white p-8 text-center text-slate-500">
          No applications yet.{' '}
          <Link to="/" className="font-semibold text-indigo-600 hover:underline">
            Browse classes
          </Link>
        </div>
      ) : (
        <ul className="flex flex-col gap-3">
          {holds.data.map((hold) => (
            <ApplicationRow key={hold.holdId} hold={hold} now={now} studentId={studentId} />
          ))}
        </ul>
      )}
    </section>
  )
}

function ApplicationRow({ hold, now, studentId }: { hold: Hold; now: number; studentId: string }) {
  const status = displayStatus(hold, now)

  return (
    <li className="flex flex-col gap-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm sm:flex-row sm:items-center sm:justify-between">
      <div className="flex flex-col gap-1.5">
        <h3 className="font-semibold">{hold.className ?? 'Class'}</h3>
        <div>
          <StatusBadge status={status} />
        </div>
        {status === 'HELD' && (
          <p className="text-xs text-slate-600">
            Time left to pay: <Countdown ms={msRemaining(hold, now)} /> · until {formatDateTime(hold.expiresAt)}
          </p>
        )}
      </div>
      <div className="flex flex-col items-start gap-2 sm:items-end">
        <HoldActions hold={hold} status={status} studentId={studentId} compact />
        <Link to={`/applications/${hold.holdId}`} className="text-sm text-indigo-600 hover:underline">
          Open details →
        </Link>
      </div>
    </li>
  )
}
