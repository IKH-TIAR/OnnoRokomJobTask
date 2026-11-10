import type { ClassSeats } from '../api/types'

/** Visual split of a class's seats: confirmed / on hold / free. */
export function SeatBar({ seats }: { seats: ClassSeats }) {
  const pct = (n: number) => (seats.totalSeats > 0 ? (n / seats.totalSeats) * 100 : 0)

  return (
    <div
      className="flex h-2 w-full overflow-hidden rounded-full bg-slate-200"
      role="img"
      aria-label={`${seats.confirmedSeats} confirmed, ${seats.heldSeats} on hold, ${seats.availableSeats} free of ${seats.totalSeats}`}
    >
      <div className="bg-green-500 transition-all duration-500" style={{ width: `${pct(seats.confirmedSeats)}%` }} />
      <div className="bg-amber-400 transition-all duration-500" style={{ width: `${pct(seats.heldSeats)}%` }} />
    </div>
  )
}
