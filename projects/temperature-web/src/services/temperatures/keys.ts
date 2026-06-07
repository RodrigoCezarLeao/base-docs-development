export const temperatureKeys = {
  all: ['temperatures'] as const,
  lists: () => [...temperatureKeys.all, 'list'] as const,
  list: (page: number, pageSize: number) => [...temperatureKeys.lists(), { page, pageSize }] as const,
  details: () => [...temperatureKeys.all, 'detail'] as const,
  detail: (id: number) => [...temperatureKeys.details(), id] as const,
}
