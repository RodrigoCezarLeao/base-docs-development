import { useShallow } from 'zustand/react/shallow'
import { useCanvasStore } from './store'
import { selectSelectedDocumentId } from './selectors'

export function useCanvasSelection() {
  return useCanvasStore(
    useShallow((state) => ({
      selectedDocumentId: selectSelectedDocumentId(state),
      selectDocument: state.selectDocument,
      clearSelection: state.clearSelection,
    })),
  )
}
