import { useEffect, useState } from 'react'
import type { Hold, HoldStatus } from '../api/types'

/** Current time, re-rendering the component every second (drives countdowns). */
export function useNow(intervalMs = 1000): number {
  const [now, setNow] = useState(() => Date.now())
  useEffect(() => {
    const id = window.setInterval(() => setNow(Date.now()), intervalMs)
    return () => window.clearInterval(id)
  }, [intervalMs])
  return now
}

export function msRemaining(hold: Hold, now: number): number {
  return Math.max(0, new Date(hold.expiresAt).getTime() - now)
}

/**
 * Status to show right now. A HELD hold whose time has run out is shown as EXPIRED
 * immediately, without waiting for the next server refresh.
 */
export function displayStatus(hold: Hold, now: number): HoldStatus {
  return hold.status === 'HELD' && msRemaining(hold, now) === 0 ? 'EXPIRED' : hold.status
}

/** 600000 → "10:00", 59000 → "0:59" */
export function formatRemaining(ms: number): string {
  const totalSeconds = Math.ceil(ms / 1000)
  const minutes = Math.floor(totalSeconds / 60)
  const seconds = totalSeconds % 60
  return `${minutes}:${seconds.toString().padStart(2, '0')}`
}

export function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'medium' })
}
