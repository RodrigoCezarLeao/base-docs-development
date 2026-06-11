import type { AccessFilters } from './types'

export const accessKeys = {
  all: ['access'] as const,
  list: (filters: AccessFilters) => [...accessKeys.all, 'list', filters] as const,
}
