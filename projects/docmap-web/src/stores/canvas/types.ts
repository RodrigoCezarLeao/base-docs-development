export interface CanvasState {
  selectedDocumentId: number | null
}

export interface CanvasActions {
  selectDocument: (id: number | null) => void
  clearSelection: () => void
}

export type CanvasStore = CanvasState & CanvasActions
