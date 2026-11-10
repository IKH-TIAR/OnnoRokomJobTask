import { ApiError } from './client'

/** Plain-language messages for backend error codes. */
const MESSAGES: Record<string, string> = {
  NETWORK_ERROR: 'Cannot reach the server. Please check your internet connection or try again in a moment.',
  SERVICE_BUSY: 'The server is very busy right now. Please try again in a few seconds.',
  INTERNAL_ERROR: 'Something went wrong on our side. Please try again.',

  CLASS_FULL: 'Sorry, this class is full. No seats are available right now.',
  CLASS_NOT_FOUND: 'This class no longer exists.',
  ALREADY_APPLIED: 'You have already applied for this class. Check "My applications".',

  HOLD_NOT_FOUND: 'This application could not be found.',
  HOLD_EXPIRED: 'Time ran out: this application has expired and the seat was released.',
  HOLD_CANCELLED: 'This application was cancelled.',
  HOLD_ALREADY_CONFIRMED: 'Admission is already confirmed (fee paid), so it cannot be cancelled.',
  HOLD_NOT_OWNED: 'This application belongs to a different student.',

  STUDENT_ID_REQUIRED: 'Please enter the student’s birth registration number at the top of the page.',
  STUDENT_ID_INVALID: 'The birth registration number must be exactly 17 digits.',

  VALIDATION_FAILED: 'Some of the information is not valid. Please check the form.',
  BAD_REQUEST: 'The request was not valid. Please check the information and try again.',
}

export function friendlyMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return MESSAGES[error.code] ?? error.message ?? 'Something went wrong. Please try again.'
  }
  return 'Something went wrong. Please try again.'
}

/** Should React Query retry automatically? Only for temporary problems, never for 4xx answers. */
export function isRetryable(error: unknown): boolean {
  return error instanceof ApiError && (error.isNetworkError || error.status >= 500)
}
