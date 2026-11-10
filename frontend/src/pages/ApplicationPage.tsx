import { useQueryClient } from '@tanstack/react-query'
import { useEffect } from 'react'
import { Link, useParams } from 'react-router'
import { ApiError } from '../api/client'
import { friendlyMessage } from '../api/errors'
import { queryKeys, useHold } from '../api/queries'
import type { HoldStatus } from '../api/types'
import { Alert } from '../components/Alert'
import { Countdown } from '../components/Countdown'
import { HoldActions } from '../components/HoldActions'
import { StatusBadge } from '../components/StatusBadge'
import { displayStatus, formatDateTime, msRemaining, useNow } from '../holds/holdTime'
import { useStudent } from '../student/studentId'

const EXPLANATIONS: Record<HoldStatus, string> = {
  HELD: 'Your seat is reserved. Pay the admission fee before the timer runs out, otherwise the seat is released to other applicants.',
  CONFIRMED: 'The admission fee has been paid. The seat is yours.',
  EXPIRED: 'Time ran out before the fee was paid, so the seat was released. You can apply again if seats are still available.',
  CANCELLED: 'This application was cancelled and the seat was released.',
}

/** One application: status, time left, "Pay admission fee" and "Cancel". */
export function ApplicationPage() {
  const { holdId = '' } = useParams()
  const { studentId } = useStudent()
  const holdQuery = useHold(holdId)
  const queryClient = useQueryClient()
  const now = useNow()

  const hold = holdQuery.data
  const status = hold ? displayStatus(hold, now) : undefined
  const expiredByClock = hold?.status === 'HELD' && status === 'EXPIRED'

  // The countdown hit zero: ask the server for the real status and fresh seat counts.
  useEffect(() => {
    if (!expiredByClock) return
    queryClient.invalidateQueries({ queryKey: queryKeys.hold(holdId) })
    queryClient.invalidateQueries({ queryKey: queryKeys.classes })
    if (studentId) queryClient.invalidateQueries({ queryKey: queryKeys.myHolds(studentId) })
  }, [expiredByClock, holdId, studentId, queryClient])

  if (holdQuery.isPending) return <p className="text-slate-500">Loading application…</p>

  if (!hold || !status) {
    const notFound = holdQuery.error instanceof ApiError && holdQuery.error.status === 404
    return (
      <div className="flex flex-col gap-3">
        <Alert kind="error">
          {notFound ? 'This application could not be found.' : `${friendlyMessage(holdQuery.error)} Retrying automatically…`}
        </Alert>
        <BackLinks />
      </div>
    )
  }

  const isOwner = studentId !== null && hold.studentId === studentId

  return (
    <section className="flex max-w-xl flex-col gap-4">
      {holdQuery.isError && (
        <Alert kind="warning">Lost connection to the server. Showing the last known status; retrying automatically…</Alert>
      )}

      <div className="flex flex-col gap-4 rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
        <div className="flex flex-col gap-1">
          <p className="text-sm text-slate-500">Application for</p>
          <h2 className="text-xl font-bold">{hold.className ?? 'Class'}</h2>
        </div>

        <div>
          <StatusBadge status={status} />
        </div>

        {status === 'HELD' && (
          <div className="flex flex-col items-start gap-1 rounded-lg bg-amber-50 p-4">
            <span className="text-sm text-amber-900">Time left to pay</span>
            <Countdown ms={msRemaining(hold, now)} large />
            <span className="text-xs text-amber-800">Seat held until {formatDateTime(hold.expiresAt)}</span>
          </div>
        )}

        <p className="text-sm text-slate-700">{EXPLANATIONS[status]}</p>

        {status === 'HELD' &&
          (isOwner ? (
            <HoldActions hold={hold} status={status} studentId={studentId} />
          ) : studentId === null ? (
            <Alert kind="info">Enter the student’s birth registration number at the top of the page to pay or cancel.</Alert>
          ) : (
            <Alert kind="warning">
              This application belongs to a different student, so you cannot pay for or cancel it with the current
              birth registration number.
            </Alert>
          ))}

        {status === 'EXPIRED' && (
          <Link to="/" className="self-start rounded-md bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700">
            See available seats
          </Link>
        )}

        <dl className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1 border-t border-slate-100 pt-4 text-xs text-slate-500">
          <dt>Application ID</dt>
          <dd className="font-mono break-all">{hold.holdId}</dd>
          {hold.studentId && (
            <>
              <dt>Birth reg. no.</dt>
              <dd className="font-mono">{hold.studentId}</dd>
            </>
          )}
        </dl>
      </div>

      <BackLinks />
    </section>
  )
}

function BackLinks() {
  return (
    <div className="flex gap-4 text-sm">
      <Link to="/my-applications" className="text-indigo-600 hover:underline">
        ← My applications
      </Link>
      <Link to="/" className="text-indigo-600 hover:underline">
        Classes
      </Link>
    </div>
  )
}
