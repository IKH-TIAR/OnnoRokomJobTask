import type { HoldStatus } from '../api/types'

const LABELS: Record<HoldStatus, { text: string; className: string }> = {
  HELD: { text: 'Seat on hold, fee pending', className: 'bg-amber-100 text-amber-800' },
  CONFIRMED: { text: 'Admission confirmed', className: 'bg-green-100 text-green-800' },
  EXPIRED: { text: 'Time ran out, seat released', className: 'bg-slate-200 text-slate-700' },
  CANCELLED: { text: 'Withdrawn, seat released', className: 'bg-red-100 text-red-700' },
}

export function StatusBadge({ status }: { status: HoldStatus }) {
  const { text, className } = LABELS[status]
  return (
    <span className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-semibold ${className}`}>
      <span className="font-mono">{status}</span>
      <span className="font-normal opacity-80">· {text}</span>
    </span>
  )
}
