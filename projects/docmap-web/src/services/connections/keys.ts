export const connectionKeys = {
  all: (projectId: number) => ['connections', projectId] as const,
  lists: (projectId: number) => [...connectionKeys.all(projectId), 'list'] as const,
  detail: (projectId: number, id: number) => [...connectionKeys.all(projectId), 'detail', id] as const,
}
