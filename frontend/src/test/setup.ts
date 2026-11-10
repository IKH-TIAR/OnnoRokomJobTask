import '@testing-library/jest-dom/vitest'

afterEach(() => {
  window.localStorage.clear()
  vi.resetAllMocks()
  vi.restoreAllMocks()
  vi.unstubAllGlobals()
})
