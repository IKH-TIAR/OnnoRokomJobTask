import { ApiError } from './client'
import { friendlyMessage, isRetryable } from './errors'

describe('friendlyMessage', () => {
  it.each([
    ['CLASS_FULL', 'this class is full'],
    ['ALREADY_APPLIED', 'already applied'],
    ['HOLD_EXPIRED', 'Time ran out'],
    ['HOLD_NOT_OWNED', 'different student'],
    ['STUDENT_ID_INVALID', '17 digits'],
    ['SERVICE_BUSY', 'very busy'],
    ['NETWORK_ERROR', 'Cannot reach the server'],
  ])('turns %s into a plain-language message', (code, expected) => {
    expect(friendlyMessage(new ApiError(409, code, 'raw server text'))).toContain(expected)
  })

  it('falls back to the server message for unknown codes', () => {
    expect(friendlyMessage(new ApiError(418, 'SOMETHING_NEW', 'Server says hi'))).toBe('Server says hi')
  })

  it('never shows technical details for non-API errors', () => {
    expect(friendlyMessage(new TypeError('x is undefined'))).toBe('Something went wrong. Please try again.')
  })
})

describe('isRetryable', () => {
  it('retries only temporary problems', () => {
    expect(isRetryable(new ApiError(0, 'NETWORK_ERROR', ''))).toBe(true)
    expect(isRetryable(new ApiError(503, 'SERVICE_BUSY', ''))).toBe(true)
    expect(isRetryable(new ApiError(404, 'HOLD_NOT_FOUND', ''))).toBe(false)
    expect(isRetryable(new ApiError(409, 'CLASS_FULL', ''))).toBe(false)
  })
})
