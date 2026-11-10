declare global {
  interface Window {
    __APP_CONFIG__?: { apiBaseUrl?: string }
  }
}

/**
 * Backend base URL, resolved in this order:
 * 1. window.__APP_CONFIG__.apiBaseUrl  (public/config.js, rewritten at Docker container start)
 * 2. VITE_API_BASE_URL                  (build-time / `npm run dev`)
 * 3. http://localhost:8080
 */
export const apiBaseUrl: string = (
  window.__APP_CONFIG__?.apiBaseUrl ||
  import.meta.env.VITE_API_BASE_URL ||
  'http://localhost:8080'
).replace(/\/+$/, '')

/** How often live data (seat counts, application status) is refreshed. */
export const POLL_INTERVAL_MS = 3000
