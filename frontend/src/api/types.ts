// Shapes of the backend API contract.

export interface SchoolClass {
  id: string
  name: string
  totalSeats: number
}

export interface ClassSeats extends SchoolClass {
  heldSeats: number
  confirmedSeats: number
  availableSeats: number
}

export type HoldStatus = 'HELD' | 'CONFIRMED' | 'EXPIRED' | 'CANCELLED'

export interface Hold {
  holdId: string
  classId: string
  className?: string
  studentId?: string
  status: HoldStatus
  /** ISO-8601 UTC timestamp */
  expiresAt: string
}

export interface CreateClassInput {
  name: string
  totalSeats: number
}
