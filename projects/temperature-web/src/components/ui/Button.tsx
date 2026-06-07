import type { ButtonHTMLAttributes } from 'react'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'danger'
  loading?: boolean
}

const styles: Record<string, React.CSSProperties> = {
  base: {
    padding: '8px 16px',
    border: 'none',
    borderRadius: '6px',
    cursor: 'pointer',
    fontWeight: 500,
    fontSize: '14px',
  },
  primary: { backgroundColor: '#2563eb', color: '#fff' },
  secondary: { backgroundColor: '#e5e7eb', color: '#374151' },
  danger: { backgroundColor: '#dc2626', color: '#fff' },
  disabled: { opacity: 0.5, cursor: 'not-allowed' },
}

export function Button({ variant = 'primary', loading = false, children, disabled, style, ...props }: ButtonProps) {
  return (
    <button
      {...props}
      disabled={disabled || loading}
      style={{ ...styles.base, ...styles[variant], ...(disabled || loading ? styles.disabled : {}), ...style }}
    >
      {loading ? '...' : children}
    </button>
  )
}
