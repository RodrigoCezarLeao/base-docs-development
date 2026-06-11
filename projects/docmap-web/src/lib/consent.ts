// LGPD tracking consent, persisted in localStorage. `null` means "not decided yet"
// (the banner is shown); only `granted` ever causes the X-Tracking-Consent header to be sent.
const KEY = 'docmap_tracking_consent'

export type ConsentDecision = 'granted' | 'denied'

export const consentStorage = {
  get: (): ConsentDecision | null => {
    const v = localStorage.getItem(KEY)
    return v === 'granted' || v === 'denied' ? v : null
  },
  set: (decision: ConsentDecision) => localStorage.setItem(KEY, decision),
  clear: () => localStorage.removeItem(KEY),
}

/** True only when the user has explicitly granted tracking consent. */
export const hasConsent = () => consentStorage.get() === 'granted'
