import { useMutation } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { consentStorage, type ConsentDecision } from '@/lib/consent'
import type { ApiResponse } from '@/types'

/** Records a consent decision server-side (legal proof) and persists it locally. */
export function useRecordConsent() {
  return useMutation({
    mutationFn: async (decision: ConsentDecision | 'withdrawn') => {
      // Persist locally first so the X-Tracking-Consent header reflects the new state.
      if (decision === 'granted') consentStorage.set('granted')
      else consentStorage.set('denied')
      return await api.post<ApiResponse<{ decision: string }>>('/api/v1/me/consent', { decision })
    },
  })
}

/** Right to erasure: hard-deletes the caller's own tracking data. Requires auth. */
export function useDeleteMyData() {
  return useMutation({
    mutationFn: () => api.delete<ApiResponse<number>>('/api/v1/me/tracking-data'),
  })
}
