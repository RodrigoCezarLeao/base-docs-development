import { create } from 'zustand'
import type { CanvasStore } from './types'

export const useCanvasStore = create<CanvasStore>()((set) => ({
  selectedDocumentId: null,

  selectDocument: (id) => set({ selectedDocumentId: id }),

  clearSelection: () => set({ selectedDocumentId: null }),
}))
