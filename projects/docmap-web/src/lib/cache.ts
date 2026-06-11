import { createSyncStoragePersister } from '@tanstack/query-sync-storage-persister'
import type { PersistQueryClientProviderProps } from '@tanstack/react-query-persist-client'

// Named cache tiers by data volatility. Spread into a query: `...cacheTiers.stable`.
// `stable` is for rarely-changing data — long freshness + persisted to localStorage.
export const cacheTiers = {
  stable: { staleTime: 1000 * 60 * 60, gcTime: 1000 * 60 * 60 * 24, meta: { persist: true } },
  standard: { staleTime: 1000 * 60 * 5, gcTime: 1000 * 60 * 30 },
  dynamic: { staleTime: 0, gcTime: 1000 * 60 },
}

const persister = createSyncStoragePersister({
  storage: window.localStorage,
  key: 'docmap-rq-cache',
})

// Persist only the stable tier (queries tagged with meta.persist) to localStorage.
// `buster` ties the persisted cache to the app version — a new deploy discards it.
export const persistOptions: PersistQueryClientProviderProps['persistOptions'] = {
  persister,
  maxAge: cacheTiers.stable.gcTime,
  buster: __APP_VERSION__,
  dehydrateOptions: {
    shouldDehydrateQuery: (query) =>
      query.state.status === 'success' && query.meta?.persist === true,
  },
}
