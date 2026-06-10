import axios from 'axios'

const axiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'http://localhost:5000',
  headers: { 'Content-Type': 'application/json' },
})

// Interceptor returns response.data — types api to reflect this (no casts in services)
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
