export interface EndpointMetric {
  endpoint: string
  count: number
  lastCalledUnixMs: number
}

export interface TrafficPoint {
  unixSeconds: number
  count: number
}

export interface MetricsSnapshot {
  activeUsers: number
  inFlight: number
  totalRequests: number
  endpoints: EndpointMetric[]
  traffic: TrafficPoint[]
}
