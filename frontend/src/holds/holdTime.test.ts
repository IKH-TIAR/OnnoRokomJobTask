import type { Hold } from '../api/types'
import { displayStatus, formatRemaining, msRemaining } from './holdTime'

const NOW = Date.parse('2026-11-10T10:00:00Z')

const hold = (status: Hold['status'], expiresAt: string): Hold => ({
  holdId: 'h1',
  classId: 'c1',
  status,
  expiresAt,
})

describe('formatRemaining', () => {
  it.each([
    [600_000, '10:00'],
    [599_001, '10:00'], // partial seconds round up, so the timer never shows 0:00 while time is left
    [61_000, '1:01'],
    [59_000, '0:59'],
    [1, '0:01'],
    [0, '0:00'],
  ])('%i ms → %s', (ms, expected) => {
    expect(formatRemaining(ms)).toBe(expected)
  })
})

describe('msRemaining', () => {
  it('counts down to the expiry time and never goes negative', () => {
    expect(msRemaining(hold('HELD', '2026-11-10T10:10:00Z'), NOW)).toBe(600_000)
    expect(msRemaining(hold('HELD', '2026-11-10T09:59:00Z'), NOW)).toBe(0)
  })
})

describe('displayStatus', () => {
  it('shows a HELD hold as EXPIRED as soon as its time runs out, without waiting for the server', () => {
    expect(displayStatus(hold('HELD', '2026-11-10T10:00:01Z'), NOW)).toBe('HELD')
    expect(displayStatus(hold('HELD', '2026-11-10T10:00:00Z'), NOW)).toBe('EXPIRED')
  })

  it('never changes a final status', () => {
    const past = '2026-11-10T09:00:00Z'
    expect(displayStatus(hold('CONFIRMED', past), NOW)).toBe('CONFIRMED')
    expect(displayStatus(hold('CANCELLED', past), NOW)).toBe('CANCELLED')
    expect(displayStatus(hold('EXPIRED', past), NOW)).toBe('EXPIRED')
  })
})
