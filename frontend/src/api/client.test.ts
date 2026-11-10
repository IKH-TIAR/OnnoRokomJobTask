import { ApiError, api } from './client'

function mockFetch(status: number, body?: unknown) {
  const fetchMock = vi.fn().mockResolvedValue(
    new Response(body === undefined ? null : JSON.stringify(body), {
      status,
      headers: { 'Content-Type': 'application/json' },
    }),
  )
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

describe('api client', () => {
  it('sends the student id in the X-Student-Id header', async () => {
    const fetchMock = mockFetch(201, { holdId: 'h1', classId: 'c1', status: 'HELD', expiresAt: '2026-11-10T10:10:00Z' })

    const hold = await api.applyForSeat('c1', '20152691234567890')

    expect(hold.status).toBe('HELD')
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe('http://localhost:8080/classes/c1/holds')
    expect(init.method).toBe('POST')
    expect(init.headers['X-Student-Id']).toBe('20152691234567890')
  })

  it('turns a Problem Details response into an ApiError with the backend code', async () => {
    mockFetch(409, { status: 409, code: 'CLASS_FULL', detail: 'Sorry, all seats in this class are taken.' })

    const error = await api.applyForSeat('c1', '20152691234567890').catch((e: unknown) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ status: 409, code: 'CLASS_FULL' })
  })

  it('keeps per-field validation errors', async () => {
    mockFetch(400, { code: 'VALIDATION_FAILED', errors: { totalSeats: ['Total seats must be between 1 and 10000.'] } })

    const error = (await api.createClass({ name: 'X', totalSeats: 0 }).catch((e: unknown) => e)) as ApiError

    expect(error.fieldErrors.totalSeats).toEqual(['Total seats must be between 1 and 10000.'])
  })

  it('reports an unreachable server as NETWORK_ERROR', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))

    const error = (await api.listClasses().catch((e: unknown) => e)) as ApiError

    expect(error.code).toBe('NETWORK_ERROR')
    expect(error.isNetworkError).toBe(true)
  })

  it('handles 204 No Content (cancel)', async () => {
    mockFetch(204)

    await expect(api.cancelHold('h1', '20152691234567890')).resolves.toBeUndefined()
  })
})
