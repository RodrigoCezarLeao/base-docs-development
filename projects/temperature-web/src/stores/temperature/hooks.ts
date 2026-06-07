import { useShallow } from 'zustand/react/shallow'
import { useTemperatureStore } from './store'
import { selectLocationFilter } from './selectors'

export function useTemperatureFilters() {
  return useTemperatureStore(
    useShallow((state) => ({
      locationFilter: selectLocationFilter(state),
      setLocationFilter: state.setLocationFilter,
      clearFilters: state.clearFilters,
    })),
  )
}
