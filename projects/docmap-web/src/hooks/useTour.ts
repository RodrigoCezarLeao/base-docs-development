import 'driver.js/dist/driver.css'
import { driver, type DriveStep } from 'driver.js'
import { useCallback, useEffect, useRef } from 'react'

interface TourLabels {
  next?: string
  prev?: string
  done?: string
}

interface UseTourOptions {
  /** Unique id; the tour auto-runs once per id (tracked in localStorage). */
  tourId: string
  autoStart?: boolean
  labels?: TourLabels
}

/**
 * Guided product tour built on driver.js. `start()` runs it on demand; it also
 * auto-runs once per `tourId` on a user's first visit.
 */
export function useTour(steps: DriveStep[], { tourId, autoStart = true, labels }: UseTourOptions) {
  const ref = useRef({ steps, labels })
  ref.current = { steps, labels }

  const start = useCallback(() => {
    const { steps, labels } = ref.current
    driver({
      showProgress: true,
      nextBtnText: labels?.next ?? 'Next',
      prevBtnText: labels?.prev ?? 'Back',
      doneBtnText: labels?.done ?? 'Done',
      steps,
    }).drive()
  }, [])

  useEffect(() => {
    if (!autoStart) return
    const key = `tour-seen:${tourId}`
    if (localStorage.getItem(key)) return
    localStorage.setItem(key, '1')
    // Defer so the highlighted elements are mounted.
    const timer = setTimeout(start, 500)
    return () => clearTimeout(timer)
  }, [autoStart, tourId, start])

  return { start }
}
