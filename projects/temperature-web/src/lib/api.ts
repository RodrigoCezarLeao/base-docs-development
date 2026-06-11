// Centralized HTTP client built on the native fetch API (no axios dependency).
// `api` is typed as ApiInstance so methods return Promise<T> — the parsed JSON body —
// directly, with no casts in services. Non-2xx responses reject with the parsed body.
// The JWT (when present) is attached as a Bearer token on every request.
import { tokenStorage } from './auth'
import { hasConsent } from './consent'

const baseURL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000'

export type RequestConfig = {
  params?: Record<string, string | number | boolean | undefined | null>
  headers?: Record<string, string>
  signal?: AbortSignal
}

type ApiInstance = {
  get<T>(url: string, config?: RequestConfig): Promise<T>
  post<T>(url: string, data?: unknown, config?: RequestConfig): Promise<T>
  put<T>(url: string, data?: unknown, config?: RequestConfig): Promise<T>
  patch<T>(url: string, data?: unknown, config?: RequestConfig): Promise<T>
  delete<T>(url: string, config?: RequestConfig): Promise<T>
}

function buildUrl(url: string, params?: RequestConfig['params']): string {
  if (!params) return `${baseURL}${url}`
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null) search.append(key, String(value))
  }
  const qs = search.toString()
  return qs ? `${baseURL}${url}?${qs}` : `${baseURL}${url}`
}

async function request<T>(method: string, url: string, data?: unknown, config?: RequestConfig): Promise<T> {
  const token = tokenStorage.get()
  const response = await fetch(buildUrl(url, config?.params), {
    method,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      // LGPD: only advertise consent when the user has explicitly granted it.
      ...(hasConsent() ? { 'X-Tracking-Consent': 'granted' } : {}),
      ...config?.headers,
    },
    body: data === undefined ? undefined : JSON.stringify(data),
    signal: config?.signal,
  })

  const text = await response.text()
  const body = text ? JSON.parse(text) : undefined

  if (!response.ok) return Promise.reject(body ?? new Error(`HTTP ${response.status}`))
  return body as T
}

export const api: ApiInstance = {
  get: <T>(url: string, config?: RequestConfig) => request<T>('GET', url, undefined, config),
  post: <T>(url: string, data?: unknown, config?: RequestConfig) => request<T>('POST', url, data, config),
  put: <T>(url: string, data?: unknown, config?: RequestConfig) => request<T>('PUT', url, data, config),
  patch: <T>(url: string, data?: unknown, config?: RequestConfig) => request<T>('PATCH', url, data, config),
  delete: <T>(url: string, config?: RequestConfig) => request<T>('DELETE', url, undefined, config),
}
