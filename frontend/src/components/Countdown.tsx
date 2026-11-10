import { formatRemaining } from '../holds/holdTime'

/** Time left to pay, turning red in the last minute. */
export function Countdown({ ms, large = false }: { ms: number; large?: boolean }) {
  const urgent = ms <= 60_000
  return (
    <span
      className={`font-mono font-bold tabular-nums ${large ? 'text-4xl' : 'text-sm'} ${
        urgent ? 'text-red-600' : 'text-slate-900'
      }`}
      aria-live="off"
    >
      {formatRemaining(ms)}
    </span>
  )
}
