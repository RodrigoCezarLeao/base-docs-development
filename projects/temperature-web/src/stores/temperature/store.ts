import { create } from 'zustand'
import type { TemperatureStore } from './types'

export const useTemperatureStore = create<TemperatureStore>((set) => ({
  locationFilter: '',

  setLocationFilter: (location) => set({ locationFilter: location }),
  clearFilters: () => set({ locationFilter: '' }),
}))
