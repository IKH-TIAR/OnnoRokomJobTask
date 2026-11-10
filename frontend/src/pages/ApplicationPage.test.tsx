import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ApiError, api } from '../api/client'
import type { Hold } from '../api/types'
import { OTHER_STUDENT, STUDENT, inMs, renderPage } from '../test/render'
import { ApplicationPage } from './ApplicationPage'

vi.mock('../api/client', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../api/client')>()
  return { ...actual, api: { getHold: vi.fn(), confirmHold: vi.fn(), cancelHold: vi.fn() } }
})

const heldHold = (overrides: Partial<Hold> = {}): Hold => ({
  holdId: 'h1',
  classId: 'c1',
  className: 'Class 6 - Morning Shift',
  studentId: STUDENT,
  status: 'HELD',
  expiresAt: inMs(10 * 60_000),
  ...overrides,
})

/** Fake server: getHold always returns the current state, which confirm/cancel change. */
function fakeServer(initial: Hold) {
  let current = initial
  vi.mocked(api.getHold).mockImplementation(async () => current)
  vi.mocked(api.confirmHold).mockImplementation(async () => (current = { ...current, status: 'CONFIRMED' }))
  vi.mocked(api.cancelHold).mockImplementation(async () => {
    current = { ...current, status: 'CANCELLED' }
  })
}

const openApplication = (studentId: string | null = STUDENT) =>
  renderPage(<ApplicationPage />, { route: '/applications/h1', path: '/applications/:holdId', studentId })

describe('ApplicationPage', () => {
  it('shows status, class and time left to pay', async () => {
    fakeServer(heldHold())
    openApplication()

    expect(await screen.findByText('HELD')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Class 6 - Morning Shift' })).toBeInTheDocument()
    expect(screen.getByText(/^(10:00|9:5\d)$/)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Pay admission fee' })).toBeEnabled()
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeEnabled()
  })

  it('switches to EXPIRED by itself when time runs out (no reload) and hides Pay', async () => {
    fakeServer(heldHold({ expiresAt: inMs(1500) }))
    openApplication()

    expect(await screen.findByText('HELD')).toBeInTheDocument()

    expect(await screen.findByText('EXPIRED', {}, { timeout: 4000 })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Pay admission fee' })).not.toBeInTheDocument()
    expect(screen.getByText(/time ran out before the fee was paid/i)).toBeInTheDocument()
    // ...and it asked the server for the real status
    expect(vi.mocked(api.getHold).mock.calls.length).toBeGreaterThan(1)
  })

  it('confirms the admission when paying', async () => {
    fakeServer(heldHold())
    const user = userEvent.setup()
    openApplication()

    await user.click(await screen.findByRole('button', { name: 'Pay admission fee' }))

    expect(api.confirmHold).toHaveBeenCalledWith('h1', STUDENT)
    expect(await screen.findByText('CONFIRMED')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Pay admission fee' })).not.toBeInTheDocument()
  })

  it('cancels after asking for confirmation', async () => {
    fakeServer(heldHold())
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)
    const user = userEvent.setup()
    openApplication()

    await user.click(await screen.findByRole('button', { name: 'Cancel' }))

    expect(confirmSpy).toHaveBeenCalled()
    expect(api.cancelHold).toHaveBeenCalledWith('h1', STUDENT)
    expect(await screen.findByText('CANCELLED')).toBeInTheDocument()
  })

  it('does not cancel when the user changes their mind', async () => {
    fakeServer(heldHold())
    vi.spyOn(window, 'confirm').mockReturnValue(false)
    const user = userEvent.setup()
    openApplication()

    await user.click(await screen.findByRole('button', { name: 'Cancel' }))

    expect(api.cancelHold).not.toHaveBeenCalled()
    expect(screen.getByText('HELD')).toBeInTheDocument()
  })

  it('explains a failed payment in plain language', async () => {
    fakeServer(heldHold())
    vi.mocked(api.confirmHold).mockRejectedValue(new ApiError(409, 'HOLD_EXPIRED', 'raw'))
    const user = userEvent.setup()
    openApplication()

    await user.click(await screen.findByRole('button', { name: 'Pay admission fee' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(/time ran out/i)
  })

  it("does not offer pay/cancel on another student's application", async () => {
    fakeServer(heldHold({ studentId: OTHER_STUDENT }))
    openApplication()

    expect(await screen.findByText(/belongs to a different student/i)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Pay admission fee' })).not.toBeInTheDocument()
  })

  it('shows a clear message for an unknown application', async () => {
    vi.mocked(api.getHold).mockRejectedValue(new ApiError(404, 'HOLD_NOT_FOUND', 'raw'))
    openApplication()

    expect(await screen.findByText(/could not be found/i)).toBeInTheDocument()
  })
})
