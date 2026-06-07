import axios from 'axios'
import { tokenStorage } from './auth'

const axiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'http://localhost:5001',
  headers: { 'Content-Type': 'application/json' },
})

axiosInstance.interceptors.request.use((config) => {
  const token = tokenStorage.get()
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// Interceptor retorna response.data — tipamos o api para refletir isso
axiosInstance.interceptors.response.use(
  (response) => response.data,
  (error) => Promise.reject(error),
)

type ApiInstance = {
  get<T>(url: string, config?: object): Promise<T>
  post<T>(url: string, data?: unknown, config?: object): Promise<T>
  put<T>(url: string, data?: unknown, config?: object): Promise<T>
  patch<T>(url: string, data?: unknown, config?: object): Promise<T>
  delete<T>(url: string, config?: object): Promise<T>
}

export const api = axiosInstance as unknown as ApiInstance
