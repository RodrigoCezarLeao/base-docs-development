export const documentKeys = {
  all: (projectId: number) => ['documents', projectId] as const,
  lists: (projectId: number) => [...documentKeys.all(projectId), 'list'] as const,
  detail: (projectId: number, id: number) => [...documentKeys.all(projectId), 'detail', id] as const,
}
