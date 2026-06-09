# Guideline — React Frontend with TypeScript, Vite, React Query, Zustand, Tailwind, and i18n

## Purpose

This document defines the organization and development standard for React frontends. The goal is to ensure compact components, organized stores, predictable queries, tested helpers, and a folder structure any developer can navigate without explanation.

---

## Stack

| Lib | Min version | Role |
|---|---|---|
| React | 18 | UI |
| TypeScript | 5 | Static typing |
| Vite | 5 | Build and dev server |
| TanStack Query (React Query) | 5 | Server state (fetch, cache, mutations) |
| Zustand | 4 | Client state (UI state, session, preferences) |
| i18next + react-i18next | 23 | Internationalization |
| Tailwind CSS | 4 | Utility-first styling |
| ESLint | 9 | Linter (flat config) |
| Vitest + Testing Library | 2.x | Unit tests |
| `@vitest/coverage-v8` | 2.x | Code coverage |
| axios | 1.x | HTTP client |

### Required configuration

**`package.json`** — declare `"type": "module"`. Without it, ESM packages like `@tailwindcss/vite` fail with a CommonJS module error:

```json
{
  "type": "module"
}
```

**`tsconfig.json`** — declare `"types": ["vite/client"]` in `compilerOptions`. Without it, `import.meta.env` has no type and TypeScript reports an error:

```json
{
  "compilerOptions": {
    "types": ["vite/client"]
  }
}
```

---

## Folder Structure

```
src/
├── assets/                    → static images, fonts, icons
├── components/
│   ├── ui/                    → base components without business logic (Button, Input, Modal, Badge)
│   └── shared/                → reusable components with some logic (UserAvatar, StatusTag)
├── features/
│   └── {feature}/
│       ├── components/        → components exclusive to this feature
│       ├── hooks/             → local hooks for this feature
│       └── index.ts           → public feature exports
├── hooks/                     → global reusable hooks (useDebounce, useLocalStorage)
├── i18n/
│   ├── locales/
│   │   ├── pt-BR/
│   │   │   └── translation.json
│   │   └── en/
│   │       └── translation.json
│   └── index.ts               → i18next configuration
├── lib/
│   ├── api.ts                 → typed axios instance (ApiInstance) + response interceptor
│   ├── queryClient.ts         → global QueryClient instance and configuration
│   └── cn.ts                  → helper for merging Tailwind classes (clsx + tailwind-merge)
├── services/
│   └── {domain}/
│       ├── types.ts           → API request/response types
│       ├── keys.ts            → domain queryKeys
│       ├── queries.ts         → useQuery hooks (read)
│       └── actions.ts         → useMutation hooks (write)
├── stores/
│   └── {domain}/
│       ├── types.ts           → state and actions interfaces
│       ├── store.ts           → createStore with state + actions
│       ├── selectors.ts       → pure selector functions
│       └── hooks.ts           → public hooks with shallow
├── helpers/
│   ├── format.ts              → date, currency, string formatting
│   ├── format.test.ts
│   ├── validators.ts          → pure validators
│   └── validators.test.ts
├── types/
│   └── index.ts               → shared global types
├── pages/
│   └── {page}/
│       └── index.tsx
├── router/
│   └── index.tsx
├── App.tsx
└── main.tsx
```

**Organization rules:**
- Everything belonging to a single feature lives in `features/{feature}/`
- Everything used by two or more features moves up to `components/shared/` or `hooks/`
- Never import directly from inside another feature — use its `index.ts`

---

## Components

### 100-line rule

A component should not exceed **100 lines**. When it does, it's a sign it's doing too much.

Separation strategies:
1. **Complex logic** → extract to a custom hook `useComponentName`
2. **Repeated or large JSX blocks** → extract to a subcomponent
3. **Long forms** → split into form sections

```tsx
// ❌ Component doing everything (200+ lines)
export function UserProfile() {
  const [tab, setTab] = useState('info')
  // 20 lines of logic...
  return (
    <div>
      {/* 80 lines of JSX */}
    </div>
  )
}

// ✅ Compact orchestrator component
export function UserProfile() {
  const { tab, setTab, user, isLoading } = useUserProfile()

  if (isLoading) return <ProfileSkeleton />

  return (
    <div className="...">
      <ProfileHeader user={user} />
      <ProfileTabs value={tab} onChange={setTab} />
      <ProfileContent tab={tab} user={user} />
    </div>
  )
}
```

### Separation: ui vs shared vs feature

```
components/ui/       → Button, Input, Select, Modal, Spinner, Badge
                        No API calls. No store. Simple props.

components/shared/   → UserAvatar, StatusBadge, PageHeader, ConfirmDialog
                        Can use i18n and domain types. No API calls.

features/{x}/
  components/        → OrderCard, OrderFilters, OrderEmptyState
                        Can use queries and stores. Feature-exclusive.
```

### Naming

| Artifact | Pattern |
|---|---|
| Component | `PascalCase` — `UserCard.tsx` |
| Hook | `camelCase` with `use` prefix — `useUserProfile.ts` |
| Type file | `camelCase` — `types.ts` |
| Feature folder | `kebab-case` — `user-profile/` |

### Responsive design: Tailwind breakpoints vs structural separation

The right approach depends on *how different* the mobile and desktop experiences are.

**Use Tailwind breakpoints** when the difference is layout or spacing only — same component, different arrangement:

```tsx
// ✅ Tailwind breakpoints — one component, responsive layout
export function UserCard({ user }: UserCardProps) {
  return (
    <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:gap-4">
      <Avatar src={user.avatar} />
      <div>
        <p className="text-sm font-semibold">{user.name}</p>
        <p className="text-xs text-gray-500 hidden sm:block">{user.email}</p>
      </div>
    </div>
  )
}
```

**Use structural separation** (`desktop/` and `mobile/` subfolders) when the component has fundamentally different JSX or interaction patterns between screen sizes — for example, a sidebar on desktop and a bottom sheet on mobile:

```
features/navigation/
  components/
    desktop/
      Sidebar.tsx          → fixed left panel with full labels
      Sidebar.test.tsx
    mobile/
      BottomSheet.tsx      → swipeable sheet with icon-only tabs
      BottomSheet.test.tsx
    index.tsx              → picks the right one based on screen size
```

```tsx
// features/navigation/components/index.tsx
import { useMediaQuery } from '@/hooks/useMediaQuery'
import { Sidebar } from './desktop/Sidebar'
import { BottomSheet } from './mobile/BottomSheet'

export function Navigation() {
  const isDesktop = useMediaQuery('(min-width: 768px)')
  return isDesktop ? <Sidebar /> : <BottomSheet />
}
```

Shared logic (data fetching, state) lives in a hook extracted to `features/navigation/hooks/` and used by both components — no duplication.

**Decision guide:**

| Situation | Approach |
|---|---|
| Different padding, font sizes, column count | Tailwind breakpoints |
| Elements hidden/shown per breakpoint | Tailwind `hidden sm:block` |
| Completely different layout structure | Tailwind breakpoints (usually still enough) |
| Different interaction pattern (sidebar vs bottom sheet, table vs cards) | `desktop/` + `mobile/` separation |
| Different navigation flow per platform | `desktop/` + `mobile/` separation |

Avoid putting `isMobile ? <BigBlockA /> : <BigBlockB />` inline in a component — extract the condition to an `index.tsx` dispatcher (as shown above) so each variant stays focused.

---

### `useMemo` vs `useEffect` — never use `useMemo` for side effects

`useMemo` computes and **returns** a value. `useEffect` runs **after** render to perform side effects. Using `useMemo` to call `setState` triggers a state update during render, which schedules another render before the current one finishes — causing infinite re-renders ("Too many re-renders").

```tsx
// ❌ useMemo calling setState — triggers infinite re-renders
useMemo(() => {
  setNodes(documents.map(docToNode))
}, [documents, setNodes])

// ✅ useEffect — runs after render, correct place for state synchronization
useEffect(() => {
  setNodes(documents.map(docToNode))
}, [documents])

// ✅ useMemo — computing a derived value, no side effects
const nodes = useMemo(
  () => documents.map(docToNode),
  [documents]
)
```

**Rule:** if the `useMemo` callback does not `return` a value, it should be a `useEffect`.

---

### ReactFlow — `connectionMode="loose"` for free-form canvases

ReactFlow handles have two types: `source` (starts connections) and `target` (receives connections). By default, the library only allows `source → target` connections — dragging from a `target` handle or dropping onto a `source` handle silently does nothing, which appears as connections only working on specific nodes.

For document maps or any canvas where connections can go in any direction, add `connectionMode={ConnectionMode.Loose}` to the `<ReactFlow>` component. In loose mode every handle acts as both source and target:

```tsx
import ReactFlow, { ConnectionMode } from 'reactflow'

<ReactFlow
  connectionMode={ConnectionMode.Loose}
  // ...other props
>
```

Without this, users can only connect by dragging from the bottom/right handles of one node to the top/left handles of another — any other combination fails silently.

---

### JSX best practices

```tsx
// ❌ Inline logic in JSX
<div>
  {items.filter(i => i.active).sort((a, b) => a.name.localeCompare(b.name)).map(item => (
    <div key={item.id}>{item.name}</div>
  ))}
</div>

// ✅ Logic outside JSX, clean JSX
const activeItems = items.filter(i => i.active).sort((a, b) => a.name.localeCompare(b.name))

return (
  <div>
    {activeItems.map(item => <ItemRow key={item.id} item={item} />)}
  </div>
)
```

---

## Zustand

### Domain-based organization

Each state domain has its own folder with 4 files:

```
stores/
└── auth/
    ├── types.ts
    ├── store.ts
    ├── selectors.ts
    └── hooks.ts
```

#### `types.ts` — separate interfaces for state and actions

```typescript
// stores/auth/types.ts

export interface AuthState {
  user: User | null
  token: string | null
  isAuthenticated: boolean
}

export interface AuthActions {
  setUser: (user: User) => void
  setToken: (token: string) => void
  logout: () => void
}

export type AuthStore = AuthState & AuthActions
```

#### `store.ts` — store creation only

```typescript
// stores/auth/store.ts
import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { AuthStore } from './types'

export const useAuthStore = create<AuthStore>()(
  persist(
    (set) => ({
      user: null,
      token: null,
      isAuthenticated: false,

      setUser: (user) => set({ user, isAuthenticated: true }),
      setToken: (token) => set({ token }),
      logout: () => set({ user: null, token: null, isAuthenticated: false }),
    }),
    { name: 'auth-storage' }
  )
)
```

#### `selectors.ts` — pure functions, no store coupling

```typescript
// stores/auth/selectors.ts
import type { AuthState } from './types'

export const selectUser = (state: AuthState) => state.user
export const selectIsAuthenticated = (state: AuthState) => state.isAuthenticated
export const selectToken = (state: AuthState) => state.token
export const selectUserRole = (state: AuthState) => state.user?.role ?? null
```

#### `hooks.ts` — single access point with `useShallow`

`useShallow` prevents unnecessary re-renders when selecting multiple values.

```typescript
// stores/auth/hooks.ts
import { useShallow } from 'zustand/react/shallow'
import { useAuthStore } from './store'
import { selectUser, selectIsAuthenticated, selectUserRole } from './selectors'

// Hook for user data — re-renders only when user or isAuthenticated changes
export function useAuth() {
  return useAuthStore(
    useShallow((state) => ({
      user: selectUser(state),
      isAuthenticated: selectIsAuthenticated(state),
      logout: state.logout,
    }))
  )
}

// Hook for a single value — useShallow not needed (primitive value)
export function useUserRole() {
  return useAuthStore(selectUserRole)
}

// Hook for pure actions — stable references, no re-render
export function useAuthActions() {
  return useAuthStore(
    useShallow((state) => ({
      setUser: state.setUser,
      setToken: state.setToken,
      logout: state.logout,
    }))
  )
}
```

**In components, always import via the hook — never the store directly:**

```tsx
// ❌ Direct store access in the component
import { useAuthStore } from '@/stores/auth/store'
const user = useAuthStore((state) => state.user)

// ✅ Access via the published hook
import { useAuth } from '@/stores/auth/hooks'
const { user } = useAuth()
```

### What to (and not to) put in Zustand

```
✅ Put in Zustand:
  - User session (user, token, role)
  - UI preferences (theme, language, sidebar open/closed)
  - Global selection state (active filters, selected item in a list)
  - Cart, multi-step wizard, onboarding state

❌ Do not put in Zustand:
  - Data from the API → use React Query
  - Local component state → use useState
  - Form state → use react-hook-form or useState
  - Request cache → use React Query
```

---

## API Client (Axios)

All backend communication goes through a centralized axios instance in `src/lib/api.ts`.

The response interceptor automatically extracts `.data` from each `AxiosResponse`. For TypeScript to understand this without requiring casts in every service, the exported object is typed with `ApiInstance` — a type that declares the methods returning `Promise<T>` directly:

```typescript
// src/lib/api.ts
import axios from 'axios'

// Tells TypeScript the interceptor already extracted .data —
// services receive T directly, without AxiosResponse<T>.
type ApiInstance = {
  get<T>(url: string, config?: object): Promise<T>
  post<T>(url: string, data?: unknown, config?: object): Promise<T>
  put<T>(url: string, data?: unknown, config?: object): Promise<T>
  patch<T>(url: string, data?: unknown, config?: object): Promise<T>
  delete<T>(url: string, config?: object): Promise<T>
}

const axiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'http://localhost:5000',
  headers: { 'Content-Type': 'application/json' },
})

axiosInstance.interceptors.response.use(
  (response) => response.data,
  (error) => Promise.reject(error),
)

export const api = axiosInstance as unknown as ApiInstance
```

With this, services call `api.get<T>()` and receive `Promise<T>` — with no casts:

```typescript
// ✅ No cast — api.get<ApiResponse<Order>> returns Promise<ApiResponse<Order>>
const order = await api.get<ApiResponse<Order>>('/orders/1')

// ❌ Without ApiInstance — TypeScript would report a type error
const order = await axiosInstance.get<ApiResponse<Order>>('/orders/1') // AxiosResponse<...>, not ApiResponse<...>
```

If the API requires JWT authentication, add the request interceptor before the response interceptor:

```typescript
axiosInstance.interceptors.request.use((config) => {
  const token = useAuthStore.getState().token
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})
```

---

## React Query

### Domain-based organization

```
services/
└── orders/
    ├── types.ts       → domain types (Order, CreateOrderDto, OrderFilters)
    ├── keys.ts        → queryKeys factory
    ├── queries.ts     → read hooks (useQuery, useInfiniteQuery)
    └── actions.ts     → write hooks (useMutation)
```

#### `types.ts`

```typescript
// services/orders/types.ts

export interface Order {
  id: number
  number: string
  total: number
  status: 'pending' | 'confirmed' | 'shipped' | 'delivered'
  createdAt: string
}

export interface CreateOrderDto {
  items: OrderItem[]
  addressId: number
}

export interface OrderFilters {
  status?: Order['status']
  page?: number
  pageSize?: number
}

export interface PagedOrders {
  items: Order[]
  totalCount: number
  page: number
  pageSize: number
}
```

#### `keys.ts` — queryKeys as factory for precise invalidation

```typescript
// services/orders/keys.ts

export const orderKeys = {
  all: ['orders'] as const,
  lists: () => [...orderKeys.all, 'list'] as const,
  list: (filters: OrderFilters) => [...orderKeys.lists(), filters] as const,
  details: () => [...orderKeys.all, 'detail'] as const,
  detail: (id: number) => [...orderKeys.details(), id] as const,
}
```

#### `queries.ts`

```typescript
// services/orders/queries.ts
import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { orderKeys } from './keys'
import type { Order, OrderFilters, PagedOrders } from './types'

export function useOrders(filters: OrderFilters = {}) {
  return useQuery({
    queryKey: orderKeys.list(filters),
    queryFn: () => api.get<PagedOrders>('/orders', { params: filters }),
  })
}

export function useOrder(id: number) {
  return useQuery({
    queryKey: orderKeys.detail(id),
    queryFn: () => api.get<Order>(`/orders/${id}`),
    enabled: !!id,
  })
}
```

#### `actions.ts`

```typescript
// services/orders/actions.ts
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { orderKeys } from './keys'
import type { CreateOrderDto, Order } from './types'

export function useCreateOrder() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (dto: CreateOrderDto) => api.post<Order>('/orders', dto),
    onSuccess: () => {
      // Invalidates all order lists
      queryClient.invalidateQueries({ queryKey: orderKeys.lists() })
    },
  })
}

export function useDeleteOrder() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: number) => api.delete(`/orders/${id}`),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: orderKeys.lists() })
      queryClient.removeQueries({ queryKey: orderKeys.detail(id) })
    },
  })
}
```

### QueryClient Configuration

```typescript
// lib/queryClient.ts
import { QueryClient } from '@tanstack/react-query'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 60 * 5,   // 5 minutes
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
})
```

---

## Pagination

The backend returns `ApiResponse<PagedResponse<T>>` with `items`, `totalCount`, `page`, `pageSize`, `totalPages`, `hasNextPage`, and `hasPreviousPage`. The frontend uses two reusable building blocks: the `usePagination` hook for state and the `<Pagination>` component for the UI.

### `usePagination` — state hook

```typescript
// hooks/usePagination.ts
import { useCallback, useState } from 'react'

interface UsePaginationOptions {
  initialPage?: number
  pageSize?: number
}

export function usePagination({ initialPage = 1, pageSize = 10 }: UsePaginationOptions = {}) {
  const [page, setPage] = useState(initialPage)

  const nextPage = useCallback(() => setPage((p) => p + 1), [])
  const prevPage = useCallback(() => setPage((p) => Math.max(1, p - 1)), [])
  const goToPage = useCallback((n: number) => setPage(n), [])
  const reset    = useCallback(() => setPage(initialPage), [initialPage])

  return { page, pageSize, setPage, nextPage, prevPage, goToPage, reset }
}
```

### `<Pagination>` — UI component

```typescript
// components/ui/Pagination.tsx
import { Button } from './Button'

interface PaginationProps {
  page: number
  totalPages: number
  onPageChange: (page: number) => void
}

export function Pagination({ page, totalPages, onPageChange }: PaginationProps) {
  if (totalPages <= 1) return null

  return (
    <nav aria-label="Pagination" className="flex items-center justify-center gap-2 mt-6">
      <Button variant="secondary" disabled={page <= 1} onClick={() => onPageChange(page - 1)}>
        ←
      </Button>
      <span className="min-w-[5rem] text-center text-sm text-gray-700">
        {page} / {totalPages}
      </span>
      <Button variant="secondary" disabled={page >= totalPages} onClick={() => onPageChange(page + 1)}>
        →
      </Button>
    </nav>
  )
}
```

### How to use in feature hooks

```typescript
// features/orders/hooks/useOrderList.ts
import { usePagination } from '@/hooks/usePagination'
import { useOrders } from '@/services/orders/queries'

export function useOrderList() {
  const { page, pageSize, setPage } = usePagination()
  const { data, isLoading, isError } = useOrders(page, pageSize)

  return {
    orders:     data?.data?.items ?? [],
    totalPages: data?.data?.totalPages ?? 1,
    isLoading,
    isError,
    page,
    setPage,
  }
}
```

```tsx
// features/orders/components/OrderList.tsx
import { Pagination } from '@/components/ui/Pagination'

export function OrderList({ orders, page, totalPages, onPageChange, isLoading, isError }) {
  if (isLoading) return <Spinner />
  if (isError)   return <p>Failed to load.</p>

  return (
    <div>
      {orders.map((o) => <OrderCard key={o.id} order={o} />)}
      <Pagination page={page} totalPages={totalPages} onPageChange={onPageChange} />
    </div>
  )
}
```

### Tests

Test the hook with `renderHook` and the component directly (no need for QueryClientProvider):

```typescript
// hooks/usePagination.test.ts
import { act, renderHook } from '@testing-library/react'
import { usePagination } from './usePagination'

it('nextPage increments by 1', () => {
  const { result } = renderHook(() => usePagination())
  act(() => result.current.nextPage())
  expect(result.current.page).toBe(2)
})

it('prevPage does not go below 1', () => {
  const { result } = renderHook(() => usePagination())
  act(() => result.current.prevPage())
  expect(result.current.page).toBe(1)
})

it('reset returns to initialPage', () => {
  const { result } = renderHook(() => usePagination({ initialPage: 3 }))
  act(() => result.current.goToPage(9))
  act(() => result.current.reset())
  expect(result.current.page).toBe(3)
})
```

```typescript
// components/ui/Pagination.test.tsx
it('renders nothing when totalPages is 1', () => {
  const { container } = render(<Pagination page={1} totalPages={1} onPageChange={vi.fn()} />)
  expect(container.firstChild).toBeNull()
})

it('disables prev button on first page', () => {
  render(<Pagination page={1} totalPages={5} onPageChange={vi.fn()} />)
  expect(screen.getByText('←').closest('button')).toBeDisabled()
})

it('calls onPageChange with page + 1 when next is clicked', async () => {
  const onChange = vi.fn()
  render(<Pagination page={3} totalPages={5} onPageChange={onChange} />)
  await userEvent.click(screen.getByText('→'))
  expect(onChange).toHaveBeenCalledWith(4)
})
```

---

## Error Boundary and Toast

### Why both together

`ErrorBoundary` catches rendering errors (component crashes) and displays a controlled fallback in place of the crash. Toast notifies the user about async operation failures (API calls), which `ErrorBoundary` does not catch because they occur outside React's render cycle.

### Dependency

```bash
pnpm add sonner
```

### `ErrorBoundary` — catches rendering errors

```tsx
// components/ui/ErrorBoundary.tsx
import { Component, type ErrorInfo, type ReactNode } from 'react'

interface Props {
  children: ReactNode
  fallback?: ReactNode
}

interface State {
  hasError: boolean
}

export class ErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false }

  static getDerivedStateFromError(): State {
    return { hasError: true }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('ErrorBoundary:', error, info.componentStack)
  }

  render() {
    if (this.state.hasError) {
      return (
        this.props.fallback ?? (
          <div className="flex flex-col items-center justify-center py-16 text-center">
            <p className="text-lg font-semibold text-gray-800">Something went wrong.</p>
            <p className="mt-1 text-sm text-gray-500">Reload the page to try again.</p>
            <button
              className="mt-4 rounded-md bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700"
              onClick={() => this.setState({ hasError: false })}
            >
              Try again
            </button>
          </div>
        )
      )
    }

    return this.props.children
  }
}
```

### Register `<Toaster>` and `<ErrorBoundary>` in `App.tsx`

```tsx
// App.tsx
import { Toaster } from 'sonner'
import { ErrorBoundary } from './components/ui/ErrorBoundary'

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ErrorBoundary>
        <HomePage />
      </ErrorBoundary>
      <Toaster position="bottom-right" richColors />
      {import.meta.env.DEV && <ReactQueryDevtools initialIsOpen={false} />}
    </QueryClientProvider>
  )
}
```

`<Toaster>` is placed **outside** the `<ErrorBoundary>` so it continues working even if the page crashes.

`<ReactQueryDevtools>` is wrapped in `import.meta.env.DEV` so it only renders during development (`pnpm dev`). Without this guard, the devtools button floats visibly in any environment — Vite removes the entire block from production builds when the condition is false.

### Toasts in mutations

Use `toast` from sonner directly in mutation `onSuccess`/`onError` callbacks. The `toast` function works outside React components, making it ideal for this use.

```typescript
// services/orders/actions.ts
import { toast } from 'sonner'

export function useCreateOrder() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (dto: CreateOrderDto) => api.post<Order>('/orders', dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: orderKeys.lists() })
      toast.success('Order created successfully.')
    },
    onError: () => {
      toast.error('Failed to create order.')
    },
  })
}
```

> **When to move the toast to the call site?** If the same mutation is used in different contexts with different messages, pass the messages as an option or call `toast` in the component's `useMutation` `onSuccess`. For the common case (one mutation, one message), keeping it in `actions.ts` is simpler.

### `ErrorBoundary` tests

```tsx
function Bomb({ explode }: { explode: boolean }) {
  if (explode) throw new Error('boom')
  return <p>ok</p>
}

it('renders default fallback when a child throws', () => {
  const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})

  render(
    <ErrorBoundary>
      <Bomb explode={true} />
    </ErrorBoundary>,
  )

  expect(screen.getByText('Something went wrong.')).toBeInTheDocument()
  consoleError.mockRestore()
})

it('recovers when "Try again" is clicked', async () => {
  const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})

  const { rerender } = render(
    <ErrorBoundary>
      <Bomb explode={true} />
    </ErrorBoundary>,
  )

  // Update the child before clearing state; otherwise the reset
  // causes a new throw and the boundary immediately returns to error state.
  rerender(
    <ErrorBoundary>
      <Bomb explode={false} />
    </ErrorBoundary>,
  )

  await userEvent.click(screen.getByText('Try again'))
  expect(screen.getByText('ok')).toBeInTheDocument()
  consoleError.mockRestore()
})
```

---

## i18n

### File structure

```
i18n/
├── locales/
│   ├── pt-BR/
│   │   └── translation.json
│   └── en/
│       └── translation.json
└── index.ts
```

Separate translations by namespace inside the JSON for easier maintenance:

```json
// i18n/locales/pt-BR/translation.json
{
  "common": {
    "save": "Salvar",
    "cancel": "Cancelar",
    "loading": "Carregando...",
    "error": "Ocorreu um erro"
  },
  "orders": {
    "title": "Pedidos",
    "empty": "Nenhum pedido encontrado",
    "status": {
      "pending": "Pendente",
      "confirmed": "Confirmado",
      "shipped": "Enviado",
      "delivered": "Entregue"
    }
  }
}
```

```typescript
// i18n/index.ts
import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import ptBR from './locales/pt-BR/translation.json'
import en from './locales/en/translation.json'

i18n.use(initReactI18next).init({
  resources: { 'pt-BR': { translation: ptBR }, en: { translation: en } },
  lng: 'pt-BR',
  fallbackLng: 'en',
  interpolation: { escapeValue: false },
})

export default i18n
```

```tsx
// Usage in component
import { useTranslation } from 'react-i18next'

export function OrdersPage() {
  const { t } = useTranslation()

  return <h1>{t('orders.title')}</h1>
}
```

---

## Helpers

Pure utility functions that don't depend on React, store, or API. Every helper must have a corresponding test file.

```
helpers/
├── format.ts
├── format.test.ts
├── validators.ts
├── validators.test.ts
├── date.ts
└── date.test.ts
```

```typescript
// helpers/format.ts

export function formatCurrency(value: number, locale = 'pt-BR', currency = 'BRL'): string {
  return new Intl.NumberFormat(locale, { style: 'currency', currency }).format(value)
}

export function formatInitials(name: string): string {
  return name
    .split(' ')
    .slice(0, 2)
    .map((n) => n[0].toUpperCase())
    .join('')
}

export function truncate(text: string, maxLength: number): string {
  if (text.length <= maxLength) return text
  return `${text.slice(0, maxLength)}...`
}
```

```typescript
// helpers/format.test.ts
import { describe, it, expect } from 'vitest'
import { formatCurrency, formatInitials, truncate } from './format'

describe('formatCurrency', () => {
  it('formats BRL correctly', () => {
    expect(formatCurrency(1500)).toBe('R$ 1.500,00')
  })
  it('handles zero', () => {
    expect(formatCurrency(0)).toBe('R$ 0,00')
  })
})

describe('formatInitials', () => {
  it('returns two initials from full name', () => {
    expect(formatInitials('João Silva')).toBe('JS')
  })
  it('returns one initial from single name', () => {
    expect(formatInitials('Ana')).toBe('A')
  })
})

describe('truncate', () => {
  it('truncates text longer than maxLength', () => {
    expect(truncate('Hello World', 5)).toBe('Hello...')
  })
  it('returns original text if within limit', () => {
    expect(truncate('Hi', 5)).toBe('Hi')
  })
})
```

**Rules:**
- Helpers are pure functions: same input → same output, no side effects
- No helper without a test
- No business logic in helpers — helpers are generic utilities

---

## Tests

### Stack

| Package | Role |
|---|---|
| `vitest` | Test runner, assertions, mocks (`vi`) |
| `@testing-library/react` | Component rendering and queries |
| `@testing-library/user-event` | Real user interaction simulation |
| `@testing-library/jest-dom` | Extra matchers (`toBeInTheDocument`, `toBeDisabled`, …) |
| `jsdom` | Virtual DOM environment |
| `@vitest/coverage-v8` | Coverage report |

### Configuration

```ts
// vite.config.ts
export default defineConfig({
  // ...
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'lcov'],
      exclude: ['src/test/**', 'src/main.tsx', '**/*.d.ts'],
    },
  },
})
```

```ts
// src/test/setup.ts
import '@testing-library/jest-dom'
```

### Test utils

Create a render utility that wraps the required providers (QueryClientProvider, etc.):

```tsx
// src/test/utils.tsx
import { render, type RenderOptions } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import type { ReactNode } from 'react'

function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0 },
      mutations: { retry: false },
    },
  })
}

function Providers({ children }: { children: ReactNode }) {
  return (
    <QueryClientProvider client={createTestQueryClient()}>
      {children}
    </QueryClientProvider>
  )
}

export function renderWithProviders(ui: React.ReactElement, options?: RenderOptions) {
  return render(ui, { wrapper: Providers, ...options })
}

export * from '@testing-library/react'
```

Import from `@/test/utils` instead of `@testing-library/react` in tests that need providers.

### Scripts

```json
{
  "scripts": {
    "test":          "vitest",
    "test:run":      "vitest run",
    "test:ui":       "vitest --ui",
    "test:coverage": "vitest run --coverage"
  }
}
```

`test` keeps the watcher active (development). `test:run` runs once — use in CI.

---

### What to test

| Layer | What to test |
|---|---|
| `helpers/` | Every pure function — same input → same output |
| `components/ui/` | Correct render, states (disabled, loading), appearance variants |
| Feature `components/` | Visible behavior: renders data, reacts to interactions |
| `services/` (API) | Calls the correct endpoint, returns the expected shape, propagates errors |
| `stores/` | Selectors and actions — test store logic directly, without a component |

**What NOT to test:**
- Internal implementation (specific CSS class names with no semantics)
- Full server integration (that's the responsibility of backend integration tests)
- Rendering details the end user doesn't see

---

### Helper tests

Simple and direct — no providers, no mocks.

```typescript
// helpers/format.test.ts
import { describe, it, expect } from 'vitest'
import { formatCelsius, celsiusToFahrenheit } from './format'

describe('formatCelsius', () => {
  it('formats with one decimal place', () => {
    expect(formatCelsius(28.5)).toBe('28.5°C')
  })
  it('handles negative values', () => {
    expect(formatCelsius(-5)).toBe('-5.0°C')
  })
})

describe('celsiusToFahrenheit', () => {
  it('converts 0°C to 32°F', () => {
    expect(celsiusToFahrenheit(0)).toBe(32)
  })
})
```

---

### Component tests

For components that use `useTranslation`, mock `react-i18next` to avoid depending on the full i18n setup:

```tsx
// components/ui/Button.test.tsx
import { render, screen } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import { Button } from './Button'

describe('Button', () => {
  it('renders children', () => {
    render(<Button>Save</Button>)
    expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument()
  })

  it('shows "..." when loading', () => {
    render(<Button loading>Save</Button>)
    expect(screen.getByRole('button')).toHaveTextContent('...')
  })

  it('is disabled when loading', () => {
    render(<Button loading>Save</Button>)
    expect(screen.getByRole('button')).toBeDisabled()
  })
})
```

```tsx
// features/temperature-list/components/TemperatureCard.test.tsx
import { render, screen } from '@testing-library/react'
import { describe, it, expect, vi } from 'vitest'
import { TemperatureCard } from './TemperatureCard'

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}))

describe('TemperatureCard', () => {
  const reading = {
    id: 1, location: 'São Paulo', valueCelsius: 28.5,
    recordedAt: new Date().toISOString(), isActive: true,
    createdAt: new Date().toISOString(), updatedAt: null,
  }

  it('renders location', () => {
    render(<TemperatureCard reading={reading} />)
    expect(screen.getByText('São Paulo')).toBeInTheDocument()
  })

  it('renders formatted temperature', () => {
    render(<TemperatureCard reading={reading} />)
    expect(screen.getByText('28.5°C')).toBeInTheDocument()
  })
})
```

---

### Form tests with userEvent

Use `userEvent.setup()` to simulate real interactions (focus, input, click):

```tsx
// features/temperature-list/components/AddTemperatureForm.test.tsx
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { AddTemperatureForm } from './AddTemperatureForm'

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}))

describe('AddTemperatureForm', () => {
  const onSubmit = vi.fn()
  beforeEach(() => onSubmit.mockClear())

  it('does not submit when fields are empty', async () => {
    const user = userEvent.setup()
    render(<AddTemperatureForm onSubmit={onSubmit} isLoading={false} />)

    await user.click(screen.getByRole('button'))

    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('calls onSubmit with correct data when form is filled', async () => {
    const user = userEvent.setup()
    render(<AddTemperatureForm onSubmit={onSubmit} isLoading={false} />)

    await user.type(screen.getByPlaceholderText('temperature.location'), 'São Paulo')
    await user.type(screen.getByPlaceholderText(/temperature\.value/), '28.5')
    await user.click(screen.getByRole('button'))

    expect(onSubmit).toHaveBeenCalledWith(
      expect.objectContaining({ location: 'São Paulo', valueCelsius: 28.5 }),
    )
  })

  it('disables the button while loading', () => {
    render(<AddTemperatureForm onSubmit={onSubmit} isLoading={true} />)
    expect(screen.getByRole('button')).toBeDisabled()
  })
})
```

---

### Service tests (API)

Use `vi.mock` to replace the `api` module — test the contract (which endpoint is called, which shape is returned) without a network dependency.

```typescript
// services/health/index.test.ts
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { ping, checkHealth } from './index'
import { api } from '@/lib/api'

vi.mock('@/lib/api', () => ({
  api: { get: vi.fn() },
}))

const mockGet = vi.mocked(api.get)

describe('ping', () => {
  beforeEach(() => mockGet.mockReset())  // mockReset clears implementations; mockClear only clears call counts

  it('calls GET /ping and returns { status: "ok" }', async () => {
    mockGet.mockResolvedValueOnce({ status: 'ok' })

    const result = await ping()

    expect(mockGet).toHaveBeenCalledWith('/ping')
    expect(result).toEqual({ status: 'ok' })
  })

  it('propagates rejection when backend is unreachable', async () => {
    mockGet.mockRejectedValueOnce(new Error('Network Error'))  // Once, not Value — see note below

    await expect(ping()).rejects.toThrow('Network Error')
  })
})

describe('checkHealth', () => {
  beforeEach(() => mockGet.mockReset())

  it('returns Healthy when all checks pass', async () => {
    mockGet.mockResolvedValueOnce({ status: 'Healthy', checks: [] })

    const result = await checkHealth()

    expect(result.status).toBe('Healthy')
  })

  it('returns Unhealthy with check details when a dependency fails', async () => {
    mockGet.mockResolvedValueOnce({
      status: 'Unhealthy',
      checks: [{ name: 'postgresql', status: 'Unhealthy', description: 'Connection refused' }],
    })

    const result = await checkHealth()

    expect(result.status).toBe('Unhealthy')
    expect(result.checks[0]).toMatchObject({ name: 'postgresql', status: 'Unhealthy' })
  })
})
```

> **Vitest 2.x — `mockReset` and `mockRejectedValueOnce`**
>
> - `mockClear()` only clears the call history (`.mock.calls`), **not** the registered implementations. Always use `mockReset()` in `beforeEach` to ensure implementations from one test don't leak into the next.
> - `mockRejectedValue(err)` (permanent variant) internally stores an immediately created `Promise.reject()`. Vitest 2.x detects this promise as "unhandled" before the test can catch it, causing the test to fail even with the correct assertion. Use `mockRejectedValueOnce(err)` — it creates the rejected promise lazily, only when the mock is called.

The service module these tests exercise:

```typescript
// services/health/index.ts
import { api } from '@/lib/api'

export interface HealthCheckResult {
  status: string
  checks: Array<{ name: string; status: string; description: string | null }>
}

export async function ping(): Promise<{ status: string }> {
  return await api.get('/ping')  // return await — required for correct rejection propagation
}

export async function checkHealth(): Promise<HealthCheckResult> {
  return await api.get('/health')
}
```

---

### React Query hook tests

To test `useQuery`/`useMutation` hooks in isolation (without rendering a full component), use `renderHook` with a wrapper that provides `QueryClientProvider`:

```typescript
// services/projects/queries.test.ts
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { createElement, type ReactNode } from 'react'
import { useProjects } from './queries'
import { api } from '@/lib/api'

vi.mock('@/lib/api', () => ({
  api: { get: vi.fn() },
}))

const mockGet = vi.mocked(api.get)

function wrapper({ children }: { children: ReactNode }) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })
  return createElement(QueryClientProvider, { client }, children)
}

describe('useProjects', () => {
  beforeEach(() => mockGet.mockReset())

  it('fetches and exposes the projects list', async () => {
    const projects = [{ id: 1, name: 'My Docs', description: null }]
    mockGet.mockResolvedValueOnce({ success: true, data: projects })

    const { result } = renderHook(() => useProjects(), { wrapper })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(mockGet).toHaveBeenCalledWith('/api/v1/projects')
    expect(result.current.data).toEqual(projects)
  })
})
```

> Create a new `QueryClient` inside the `wrapper` function for each test — this ensures cache isolation between tests.

---

### Test file structure

Place test files **next to** the file they test, with the `.test.ts(x)` suffix:

```
src/
├── components/ui/
│   ├── Button.tsx
│   └── Button.test.tsx
├── features/temperature-list/components/
│   ├── TemperatureCard.tsx
│   ├── TemperatureCard.test.tsx
│   ├── AddTemperatureForm.tsx
│   └── AddTemperatureForm.test.tsx
├── helpers/
│   ├── format.ts
│   └── format.test.ts
├── services/health/
│   ├── index.ts
│   └── index.test.ts
└── test/
    ├── setup.ts      → global setupFiles
    └── utils.tsx     → renderWithProviders and re-exports
```

---

## Tailwind CSS

### Setup (v4)

Tailwind v4 needs no configuration file — it works via the Vite plugin and a single CSS directive.

```ts
// vite.config.ts
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  plugins: [react(), tailwindcss()],
})
```

```css
/* src/styles/global.css */
@import "tailwindcss";
```

### Helper `cn`

Always use `cn()` for conditional classes. It combines `clsx` (conditional logic) with `tailwind-merge` (resolves Tailwind class conflicts).

```ts
// src/lib/cn.ts
import { clsx, type ClassValue } from 'clsx'
import { twMerge } from 'tailwind-merge'

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}
```

```tsx
// Usage in components
import { cn } from '@/lib/cn'

<button className={cn('px-4 py-2 rounded-md', isActive && 'bg-blue-600 text-white')} />
```

### Conventions

```
✅ Use Tailwind classes for all styling
✅ Use cn() for conditional classes or those combined with props
✅ Extract long combinations into variables before JSX

❌ Never use inline style={{ }} — except for genuinely dynamic values (e.g., runtime-computed width percentages)
❌ Never mix Tailwind with external CSS on the same element
❌ Never duplicate conflicting classes (e.g., `text-sm text-lg`) — use cn() which resolves the conflict
```

```tsx
// ❌ Unnecessary inline style
<div style={{ display: 'flex', gap: '8px', marginBottom: '16px' }}>

// ✅ Tailwind
<div className="flex gap-2 mb-4">

// ✅ Conditional with cn()
<div className={cn('flex gap-2', isOpen ? 'mb-4' : 'mb-0')}>

// ✅ Genuinely dynamic value (computed in JS)
<div className="relative" style={{ width: `${progress}%` }}>
```

### Component variants with `cn`

```tsx
interface BadgeProps {
  variant?: 'default' | 'success' | 'danger'
  children: React.ReactNode
}

const variantClasses = {
  default: 'bg-gray-100 text-gray-700',
  success: 'bg-green-100 text-green-700',
  danger:  'bg-red-100 text-red-700',
}

export function Badge({ variant = 'default', children }: BadgeProps) {
  return (
    <span className={cn('px-2 py-0.5 rounded text-xs font-medium', variantClasses[variant])}>
      {children}
    </span>
  )
}
```

---

## ESLint

### Configuration (flat config, v9)

```js
// eslint.config.js
import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import tseslint from 'typescript-eslint'

export default tseslint.config(
  { ignores: ['dist', 'node_modules'] },
  {
    extends: [js.configs.recommended, ...tseslint.configs.recommended],
    files: ['**/*.{ts,tsx}'],
    languageOptions: {
      ecmaVersion: 2020,
      globals: globals.browser,
    },
    plugins: {
      'react-hooks': reactHooks,
      'react-refresh': reactRefresh,
    },
    rules: {
      ...reactHooks.configs.recommended.rules,
      'react-refresh/only-export-components': ['warn', { allowConstantExport: true }],
      '@typescript-eslint/no-unused-vars': ['error', { argsIgnorePattern: '^_' }],
      '@typescript-eslint/no-explicit-any': 'error',
      'no-console': ['warn', { allow: ['warn', 'error'] }],
    },
  },
)
```

### Scripts

```json
{
  "scripts": {
    "lint": "eslint .",
    "lint:fix": "eslint . --fix"
  }
}
```

### Important rules

| Rule | Why |
|---|---|
| `no-explicit-any` as error | Forces correct typing — use `unknown` and narrowing |
| `no-unused-vars` as error | Keeps code clean; prefix intentional ones with `_` |
| `react-hooks/rules-of-hooks` | Ensures hooks are called in the correct order |
| `react-hooks/exhaustive-deps` | Prevents silent bugs in `useEffect` with missing deps |
| `no-console` as warning | Debug logs should not go to production |

---

## TypeScript — Conventions

```typescript
// ✅ Domain types in types.ts for each service or feature
// ✅ Global types (User, Pagination, ApiResponse) in src/types/index.ts
// ✅ Component props defined as interface in the same file
// ❌ Never use `any` — prefer `unknown` and narrowing

// Props interface alongside the component
interface UserCardProps {
  user: User
  onSelect?: (id: number) => void
  compact?: boolean
}

export function UserCard({ user, onSelect, compact = false }: UserCardProps) {
  // ...
}
```

---

## Security — `.npmrc`

Every frontend project must include a `.npmrc` at the project root:

```ini
# Always resolve from the public npm registry.
# Prevents corporate or local registry configs from leaking into this project.
registry=https://registry.npmjs.org/

# Block all install scripts (postinstall, preinstall, prepare) by default.
# Packages that require native binaries must be explicitly listed in pnpm-workspace.yaml
# under onlyBuiltDependencies (pnpm 11+). See the section below.
ignore-scripts=true

# Save exact versions when adding packages — no ^ or ~ ranges.
save-exact=true

# Always generate and respect the lockfile. Use --frozen-lockfile in CI.
lockfile=true

# Disable hoisting — each package only accesses its declared dependencies.
# Prevents phantom dependency bugs and unintended cross-package access.
shamefully-hoist=false

# Silence peer dependency warnings that don't block runtime.
strict-peer-dependencies=false
```

### What each setting does

| Setting | Effect |
|---|---|
| `registry` | Pins to public npm; ignores corporate/local overrides on the developer's machine |
| `ignore-scripts` | Blocks `postinstall`/`preinstall` — the main vector for supply chain attacks |
| `save-exact` | `pnpm add X` saves `"X": "1.2.3"` instead of `"X": "^1.2.3"` |
| `lockfile=true` | Always generates `pnpm-lock.yaml`; prevents silent resolution drift |
| `shamefully-hoist=false` | pnpm strict mode — no phantom dependencies via accidental hoisting |
| `strict-peer-dependencies=false` | Avoids noisy warnings from packages with loose peer dep declarations |

### Native packages allowlist (`pnpm-workspace.yaml`)

`ignore-scripts=true` blocks all install scripts, including those needed by packages with native binaries. Without an allowlist, `esbuild` (used by Vite) and `@tailwindcss/oxide` (Tailwind v4's Rust engine) won't install their platform binaries and the project won't build.

**pnpm 11+ change:** The `pnpm` field in `package.json` is no longer read by pnpm 11. The build approval mechanism also changed: `onlyBuiltDependencies` (list) was replaced by `allowBuilds` (map with `true/false`). Using the old form silently does nothing and `pnpm install` fails with `[ERR_PNPM_IGNORED_BUILDS]`.

**Node.js requirement:** pnpm 11 requires Node.js >= 22.13. It uses the `node:sqlite` built-in module which only exists from Node 22.13 onwards. Running pnpm 11 under Node 18 or 20 crashes immediately with `ERR_UNKNOWN_BUILTIN_MODULE`. Use `node-version: 22` in CI and `"node": ">=22.13.0"` in `engines`.

Create `pnpm-workspace.yaml` at the project root:

```yaml
# pnpm 11+ uses allowBuilds (map format) instead of onlyBuiltDependencies (list format).
# Set true to allow install scripts for packages that need native binaries.
# All others are blocked by ignore-scripts=true in .npmrc.
allowBuilds:
  esbuild: true
  "@tailwindcss/oxide": true
```

This is safer than `ignore-scripts=false` because the allowlist is auditable and committed to the repo — any addition to it is visible in code review.

### What isn't supported as client config

Some common recommendations don't have a direct pnpm/npm client config equivalent:

- **`min-release-age=N`** — prevents packages published less than N days ago from being installed (guards against "protestware" and fast supply chain attacks). Valid security practice but requires a registry proxy (e.g., Verdaccio) — not supported by the pnpm client directly.
- **`allow-git=none`** — prevents `git+https://` or `github:` URLs in `package.json`. Not a native config key; enforce via PR review or a pre-commit hook that rejects non-registry dependencies.

---

## Checklist for a new project

- [ ] `"type": "module"` declared in `package.json`
- [ ] `"types": ["vite/client"]` in `compilerOptions` of `tsconfig.json`
- [ ] `.npmrc` created with registry pin, `ignore-scripts`, `save-exact`, `lockfile`, `shamefully-hoist=false`
- [ ] `pnpm-workspace.yaml` created at root with `allowBuilds: { esbuild: true, "@tailwindcss/oxide": true }` (pnpm 11+ — do NOT use `onlyBuiltDependencies` or the `pnpm` field in `package.json`)
- [ ] `"engines": { "node": ">=22.13.0", "pnpm": ">=11.0.0" }` in `package.json` — pnpm 11 requires Node 22.13+ (uses `node:sqlite` built-in not available in older versions)
- [ ] All dependency versions exact (no `^` or `~`) in `package.json`
- [ ] `src/lib/api.ts` created with `ApiInstance` + response interceptor (extracts `.data`)
- [ ] If JWT is needed: request interceptor in `api.ts` reads token from Zustand store

## Checklist for a new feature

- [ ] `features/{feature}/` folder created with `components/`, `hooks/`, `index.ts`
- [ ] API types in `services/{feature}/types.ts`
- [ ] queryKeys in `services/{feature}/keys.ts`
- [ ] Queries in `services/{feature}/queries.ts`
- [ ] Mutations in `services/{feature}/actions.ts`
- [ ] If global state is needed: store in `stores/{feature}/` with the 4 files
- [ ] Components with at most 100 lines — separate logic into hooks and JSX into subcomponents
- [ ] If the component has different interaction patterns per screen size: use `desktop/` + `mobile/` subfolders with an `index.tsx` dispatcher; if it's only layout/spacing differences, use Tailwind breakpoints
- [ ] Styling via Tailwind classes — no inline `style={{}}` except for dynamically computed JS values
- [ ] Conditional classes using `cn()` from `@/lib/cn`
- [ ] User-visible text using `t()` from i18n
- [ ] New utility functions in `helpers/` with unit tests
- [ ] New API services in `services/{domain}/` with contract tests (`vi.mock`)
- [ ] Service tests use `mockReset()` in `beforeEach` and `mockRejectedValueOnce()` (not `mockRejectedValue`)
- [ ] Async service functions use `return await` (not `return`) for correct error propagation
- [ ] Feature public exports via `index.ts`

## Checklist for a new reusable component

- [ ] Will be used in more than one feature? → `components/shared/`
- [ ] Is it a base element without business logic? → `components/ui/`
- [ ] Props typed with `interface` in the same file
- [ ] No direct store or API calls if in `ui/`
- [ ] No inline logic in JSX — pre-compute before `return`
- [ ] Appearance variants via `cn()` and class object, not `style={{}}`
- [ ] `pnpm lint` passes without errors before committing
- [ ] `pnpm test:run` passes without errors before committing
