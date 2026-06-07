export interface TemperatureState {
  locationFilter: string
}

export interface TemperatureActions {
  setLocationFilter: (location: string) => void
  clearFilters: () => void
}

export type TemperatureStore = TemperatureState & TemperatureActions
