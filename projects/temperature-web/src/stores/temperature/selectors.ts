import type { TemperatureState } from './types'

export const selectLocationFilter = (state: TemperatureState) => state.locationFilter
