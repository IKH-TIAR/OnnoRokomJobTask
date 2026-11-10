import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ApiError, api } from '../api/client'
import type { ClassSeats } from '../api/types'
import { STUDENT, renderPage } from '../test/render'
import { ClassesPage } from './ClassesPage'

vi.mock('../api/client', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../api/client')>()
  return { ...actual, api: { listClasses: vi.fn(), applyForSeat: vi.fn() } }
})

const OPEN: ClassSeats = {
  id: 'c-open',
  name: 'Class 6 - Morning Shift',
  totalSeats: 120,
  heldSeats: 15,
  confirmedSeats: 80,
  availableSeats: 25,
}
const FULL: ClassSeats = {
  id: 'c-full',
  name: 'Class 6 - Day Shift',
  totalSeats: 2,
  heldSeats: 1,
  confirmedSeats: 1,
  availableSeats: 0,
}

const card = (name: string) => screen.getByRole('heading', { name }).closest('li')!

describe('ClassesPage', () => {
  beforeEach(() => {
    vi.mocked(api.listClasses).mockResolvedValue([OPEN, FULL])
  })

  it('shows every class with its seat counts', async () => {
    renderPage(<ClassesPage />)

    await screen.findByRole('heading', { name: OPEN.name })
    const open = within(card(OPEN.name))
    expect(open.getByText('120')).toBeInTheDocument()
    expect(open.getByText('15')).toBeInTheDocument()
    expect(open.getByText('80')).toBeInTheDocument()
    expect(open.getByText('25 free')).toBeInTheDocument()
  })

  it('disables Apply for a full class', async () => {
    renderPage(<ClassesPage />)

    await screen.findByRole('heading', { name: FULL.name })
    expect(within(card(FULL.name)).getByRole('button', { name: 'No seats left' })).toBeDisabled()
    expect(within(card(OPEN.name)).getByRole('button', { name: 'Apply' })).toBeEnabled()
  })

  it('asks for the student id before allowing to apply', async () => {
    renderPage(<ClassesPage />, { studentId: null })

    await screen.findByRole('heading', { name: OPEN.name })
    expect(screen.getByText(/enter the student’s birth registration number/i)).toBeInTheDocument()
    expect(within(card(OPEN.name)).getByRole('button', { name: 'Apply' })).toBeDisabled()
  })

  it('applies with the student id and opens the application', async () => {
    vi.mocked(api.applyForSeat).mockResolvedValue({
      holdId: 'h1',
      classId: OPEN.id,
      status: 'HELD',
      expiresAt: new Date(Date.now() + 600_000).toISOString(),
    })
    const user = userEvent.setup()
    renderPage(<ClassesPage />)

    await user.click(await within(await waitForCard(OPEN.name)).findByRole('button', { name: 'Apply' }))

    expect(api.applyForSeat).toHaveBeenCalledWith(OPEN.id, STUDENT)
    expect(await screen.findByText('other page')).toBeInTheDocument() // navigated to /applications/h1
  })

  it('explains why applying failed when the class just became full', async () => {
    vi.mocked(api.applyForSeat).mockRejectedValue(new ApiError(409, 'CLASS_FULL', 'raw'))
    const user = userEvent.setup()
    renderPage(<ClassesPage />)

    await user.click(within(await waitForCard(OPEN.name)).getByRole('button', { name: 'Apply' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(/this class is full/i)
  })

  it('shows a friendly message instead of a blank page when the server is down', async () => {
    vi.mocked(api.listClasses).mockRejectedValue(new ApiError(0, 'NETWORK_ERROR', 'raw'))
    renderPage(<ClassesPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent(/cannot reach the server/i)
  })
})

async function waitForCard(name: string) {
  await screen.findByRole('heading', { name })
  return card(name)
}
