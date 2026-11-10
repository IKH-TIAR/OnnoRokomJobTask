import type { ReactNode } from 'react'

const STYLES = {
  error: 'border-red-200 bg-red-50 text-red-800',
  warning: 'border-amber-200 bg-amber-50 text-amber-900',
  success: 'border-green-200 bg-green-50 text-green-800',
  info: 'border-sky-200 bg-sky-50 text-sky-900',
} as const

interface AlertProps {
  kind: keyof typeof STYLES
  children: ReactNode
  onClose?: () => void
}

export function Alert({ kind, children, onClose }: AlertProps) {
  return (
    <div
      role={kind === 'error' || kind === 'warning' ? 'alert' : 'status'}
      className={`flex items-start justify-between gap-4 rounded-lg border px-4 py-3 text-sm ${STYLES[kind]}`}
    >
      <div>{children}</div>
      {onClose && (
        <button type="button" onClick={onClose} className="shrink-0 font-semibold opacity-60 hover:opacity-100" aria-label="Dismiss">
          ✕
        </button>
      )}
    </div>
  )
}
