import { useState } from 'react'
import { friendlyMessage } from '../api/errors'
import { useCancelHold, useConfirmHold } from '../api/queries'
import type { Hold, HoldStatus } from '../api/types'
import { Alert } from './Alert'

interface HoldActionsProps {
  hold: Hold
  /** Status as currently displayed (already EXPIRED if the countdown hit zero). */
  status: HoldStatus
  studentId: string
  compact?: boolean
}

/** "Pay admission fee" and "Cancel" for a HELD application owned by the current student. */
export function HoldActions({ hold, status, studentId, compact = false }: HoldActionsProps) {
  const confirm = useConfirmHold(studentId)
  const cancel = useCancelHold(studentId)
  const [error, setError] = useState<unknown>(null)
  const busy = confirm.isPending || cancel.isPending
  const canAct = status === 'HELD'

  function pay() {
    setError(null)
    confirm.mutate(hold.holdId, { onError: setError })
  }

  function withdraw() {
    if (!window.confirm('Cancel this application? The seat will be released to other applicants.')) return
    setError(null)
    cancel.mutate(hold.holdId, { onError: setError })
  }

  const size = compact ? 'px-3 py-1.5 text-xs' : 'px-5 py-2.5 text-sm'

  return (
    <div className="flex flex-col gap-2">
      {canAct && (
        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            onClick={pay}
            disabled={busy}
            className={`rounded-md bg-green-600 font-semibold text-white hover:bg-green-700 disabled:bg-slate-300 ${size}`}
          >
            {confirm.isPending ? 'Paying…' : 'Pay admission fee'}
          </button>
          <button
            type="button"
            onClick={withdraw}
            disabled={busy}
            className={`rounded-md border border-red-300 bg-white font-semibold text-red-700 hover:bg-red-50 disabled:opacity-50 ${size}`}
          >
            {cancel.isPending ? 'Cancelling…' : 'Cancel'}
          </button>
        </div>
      )}
      {error !== null && (
        <Alert kind="error" onClose={() => setError(null)}>
          {friendlyMessage(error)}
        </Alert>
      )}
    </div>
  )
}
