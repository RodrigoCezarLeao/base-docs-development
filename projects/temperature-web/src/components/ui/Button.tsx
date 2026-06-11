import type { ButtonHTMLAttributes } from 'react'
import { cn } from '@/lib/cn'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'danger'
  loading?: boolean
}

const variantClasses = {
  primary: 'bg-blue-600 text-white hover:bg-blue-700 dark:bg-blue-500 dark:hover:bg-blue-600',
  secondary: 'bg-gray-200 text-gray-700 hover:bg-gray-300 dark:bg-gray-700 dark:text-gray-200 dark:hover:bg-gray-600',
  danger: 'bg-red-600 text-white hover:bg-red-700 dark:bg-red-500 dark:hover:bg-red-600',
}

export function Button({ variant = 'primary', loading = false, children, disabled, className, ...props }: ButtonProps) {
  const isDisabled = disabled || loading

  return (
    <button
      {...props}
      disabled={isDisabled}
      className={cn(
        'px-4 py-2 rounded-md font-medium text-sm border-0 cursor-pointer transition-colors',
        variantClasses[variant],
        isDisabled && 'opacity-50 cursor-not-allowed',
        className,
      )}
    >
      {loading ? '...' : children}
    </button>
  )
}
