import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { STUDENT, renderPage } from '../test/render'
import { StudentIdBar } from './StudentIdBar'

describe('StudentIdBar', () => {
  it('accepts digits only and rejects an id that is not 17 digits', async () => {
    const user = userEvent.setup()
    renderPage(<StudentIdBar />, { studentId: null })

    const input = screen.getByLabelText(/birth registration number/i)
    await user.type(input, '12ab3')
    expect(input).toHaveValue('123')

    await user.click(screen.getByRole('button', { name: 'Continue' }))
    expect(screen.getByText(/exactly 17 digits/i)).toBeInTheDocument()
    expect(window.localStorage.getItem('schoolAdmission.studentId')).toBeNull()
  })

  it('saves a valid id and remembers it (survives a page refresh)', async () => {
    const user = userEvent.setup()
    const { unmount } = renderPage(<StudentIdBar />, { studentId: null })

    await user.type(screen.getByLabelText(/birth registration number/i), STUDENT)
    await user.click(screen.getByRole('button', { name: 'Continue' }))

    expect(screen.getByText(STUDENT)).toBeInTheDocument()
    expect(window.localStorage.getItem('schoolAdmission.studentId')).toBe(STUDENT)

    // "Refresh": mount the app again from scratch
    unmount()
    renderPage(<StudentIdBar />, { studentId: null })
    expect(screen.getByText(STUDENT)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Change' })).toBeInTheDocument()
  })
})
