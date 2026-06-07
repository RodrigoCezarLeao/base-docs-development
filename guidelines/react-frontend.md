# Guideline — Frontend React com TypeScript, Vite, React Query, Zustand, Tailwind e i18n

## Propósito

Este documento define o padrão de organização e desenvolvimento para frontends React. O objetivo é garantir componentes compactos, stores organizadas, queries previsíveis, helpers testados e uma estrutura de pastas que qualquer desenvolvedor consiga navegar sem explicação.

---

## Stack

| Lib | Versão mínima | Papel |
|---|---|---|
| React | 18 | UI |
| TypeScript | 5 | Tipagem estática |
| Vite | 5 | Build e dev server |
| TanStack Query (React Query) | 5 | Estado servidor (fetch, cache, mutations) |
| Zustand | 4 | Estado cliente (UI state, sessão, preferências) |
| i18next + react-i18next | 23 | Internacionalização |
| Tailwind CSS | 4 | Estilização utility-first |
| ESLint | 9 | Linter (flat config) |
| Vitest + Testing Library | latest | Testes unitários |

---

## Estrutura de Pastas

```
src/
├── assets/                    → imagens, fontes, ícones estáticos
├── components/
│   ├── ui/                    → componentes base sem lógica de negócio (Button, Input, Modal, Badge)
│   └── shared/                → componentes reutilizáveis com alguma lógica (UserAvatar, StatusTag)
├── features/
│   └── {feature}/
│       ├── components/        → componentes exclusivos desta feature
│       ├── hooks/             → hooks locais desta feature
│       └── index.ts           → exports públicos da feature
├── hooks/                     → hooks globais reutilizáveis (useDebounce, useLocalStorage)
├── i18n/
│   ├── locales/
│   │   ├── pt-BR/
│   │   │   └── translation.json
│   │   └── en/
│   │       └── translation.json
│   └── index.ts               → configuração do i18next
├── lib/
│   ├── queryClient.ts         → instância e configuração global do QueryClient
│   └── cn.ts                  → helper para merge de classes Tailwind (clsx + tailwind-merge)
├── services/
│   └── {dominio}/
│       ├── types.ts           → tipos de request/response da API
│       ├── keys.ts            → queryKeys do domínio
│       ├── queries.ts         → hooks useQuery (leitura)
│       └── actions.ts         → hooks useMutation (escrita)
├── stores/
│   └── {dominio}/
│       ├── types.ts           → interfaces do estado e das actions
│       ├── store.ts           → createStore com estado + actions
│       ├── selectors.ts       → funções seletoras puras
│       └── hooks.ts           → hooks públicos com shallow
├── helpers/
│   ├── format.ts              → formatação de datas, moeda, strings
│   ├── format.test.ts
│   ├── validators.ts          → validações puras
│   └── validators.test.ts
├── types/
│   └── index.ts               → tipos globais compartilhados
├── pages/
│   └── {pagina}/
│       └── index.tsx
├── router/
│   └── index.tsx
├── App.tsx
└── main.tsx
```

**Regras de organização:**
- Tudo que pertence a uma única feature vive em `features/{feature}/`
- Tudo que é usado por duas ou mais features sobe para `components/shared/` ou `hooks/`
- Nunca importar de dentro de outra feature diretamente — use o `index.ts` dela

---

## Componentes

### Regra dos 100 linhas

Um componente não deve ultrapassar **100 linhas**. Quando isso acontece, é sinal de que ele está fazendo coisas demais.

Estratégias de separação:
1. **Lógica complexa** → extrair para um custom hook `useNomeDoComponente`
2. **Bloco de JSX repetido ou grande** → extrair para um subcomponente
3. **Formulário longo** → separar em seções de formulário

```tsx
// ❌ Componente fazendo tudo (200+ linhas)
export function UserProfile() {
  const [tab, setTab] = useState('info')
  // 20 linhas de lógica...
  return (
    <div>
      {/* 80 linhas de JSX */}
    </div>
  )
}

// ✅ Componente orquestrador compacto
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

### Separação: ui vs shared vs feature

```
components/ui/       → Button, Input, Select, Modal, Spinner, Badge
                        Sem chamada de API. Sem store. Props simples.

components/shared/   → UserAvatar, StatusBadge, PageHeader, ConfirmDialog
                        Pode usar i18n e tipos de domínio. Sem chamada de API.

features/{x}/
  components/        → OrderCard, OrderFilters, OrderEmptyState
                        Pode usar queries e stores. Exclusivo da feature.
```

### Nomenclatura

| Artefato | Padrão |
|---|---|
| Componente | `PascalCase` — `UserCard.tsx` |
| Hook | `camelCase` com prefixo `use` — `useUserProfile.ts` |
| Arquivo de tipo | `camelCase` — `types.ts` |
| Pasta de feature | `kebab-case` — `user-profile/` |

### Boas práticas de JSX

```tsx
// ❌ Lógica inline no JSX
<div>
  {items.filter(i => i.active).sort((a, b) => a.name.localeCompare(b.name)).map(item => (
    <div key={item.id}>{item.name}</div>
  ))}
</div>

// ✅ Lógica fora do JSX, JSX limpo
const activeItems = items.filter(i => i.active).sort((a, b) => a.name.localeCompare(b.name))

return (
  <div>
    {activeItems.map(item => <ItemRow key={item.id} item={item} />)}
  </div>
)
```

---

## Zustand

### Organização por domínio

Cada domínio de estado tem sua própria pasta com 4 arquivos:

```
stores/
└── auth/
    ├── types.ts
    ├── store.ts
    ├── selectors.ts
    └── hooks.ts
```

#### `types.ts` — interfaces separadas para estado e actions

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

#### `store.ts` — apenas criação do store

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

#### `selectors.ts` — funções puras, sem acoplamento ao store

```typescript
// stores/auth/selectors.ts
import type { AuthState } from './types'

export const selectUser = (state: AuthState) => state.user
export const selectIsAuthenticated = (state: AuthState) => state.isAuthenticated
export const selectToken = (state: AuthState) => state.token
export const selectUserRole = (state: AuthState) => state.user?.role ?? null
```

#### `hooks.ts` — único ponto de acesso com `useShallow`

`useShallow` evita re-renders desnecessários quando se seleciona múltiplos valores.

```typescript
// stores/auth/hooks.ts
import { useShallow } from 'zustand/react/shallow'
import { useAuthStore } from './store'
import { selectUser, selectIsAuthenticated, selectUserRole } from './selectors'

// Hook para dados do usuário — re-renderiza só quando user ou isAuthenticated mudar
export function useAuth() {
  return useAuthStore(
    useShallow((state) => ({
      user: selectUser(state),
      isAuthenticated: selectIsAuthenticated(state),
      logout: state.logout,
    }))
  )
}

// Hook para um único valor — useShallow não é necessário (valor primitivo)
export function useUserRole() {
  return useAuthStore(selectUserRole)
}

// Hook para actions puras — stable references, sem re-render
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

**Nos componentes, sempre importar pelo hook — nunca o store diretamente:**

```tsx
// ❌ Acesso direto ao store no componente
import { useAuthStore } from '@/stores/auth/store'
const user = useAuthStore((state) => state.user)

// ✅ Acesso pelo hook publicado
import { useAuth } from '@/stores/auth/hooks'
const { user } = useAuth()
```

### O que colocar (e não colocar) no Zustand

```
✅ Colocar no Zustand:
  - Sessão do usuário (user, token, role)
  - Preferências de UI (tema, idioma, sidebar aberta/fechada)
  - Estado de seleção global (filtros ativos, item selecionado numa lista)
  - Carrinho, wizard multi-step, estado de onboarding

❌ Não colocar no Zustand:
  - Dados que vêm da API → use React Query
  - Estado local de um componente → use useState
  - Estado de formulário → use react-hook-form ou useState
  - Cache de requisições → use React Query
```

---

## React Query

### Organização por domínio

```
services/
└── orders/
    ├── types.ts       → tipos de domínio (Order, CreateOrderDto, OrderFilters)
    ├── keys.ts        → queryKeys factory
    ├── queries.ts     → hooks de leitura (useQuery, useInfiniteQuery)
    └── actions.ts     → hooks de escrita (useMutation)
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

#### `keys.ts` — queryKeys como factory para invalidação precisa

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
      // Invalida todas as listas de orders
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

### Configuração do QueryClient

```typescript
// lib/queryClient.ts
import { QueryClient } from '@tanstack/react-query'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 60 * 5,   // 5 minutos
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
})
```

---

## i18n

### Estrutura de arquivos

```
i18n/
├── locales/
│   ├── pt-BR/
│   │   └── translation.json
│   └── en/
│       └── translation.json
└── index.ts
```

Separe as traduções por namespace dentro do JSON para facilitar manutenção:

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
// Uso no componente
import { useTranslation } from 'react-i18next'

export function OrdersPage() {
  const { t } = useTranslation()

  return <h1>{t('orders.title')}</h1>
}
```

---

## Helpers

Funções utilitárias puras que não dependem de React, store ou API. Toda helper deve ter arquivo de teste correspondente.

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
    expect(formatCurrency(1500)).toBe('R$ 1.500,00')
  })
  it('handles zero', () => {
    expect(formatCurrency(0)).toBe('R$ 0,00')
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

**Regras:**
- Helpers são funções puras: mesmo input → mesmo output, sem efeitos colaterais
- Nenhuma helper sem teste
- Nenhuma lógica de negócio em helper — helpers são utilitários genéricos

---

## Tailwind CSS

### Setup (v4)

Tailwind v4 não precisa de arquivo de configuração — funciona via plugin do Vite e uma única diretiva no CSS.

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

Use sempre `cn()` para classes condicionais. Ele combina `clsx` (lógica condicional) com `tailwind-merge` (resolve conflitos de classes Tailwind).

```ts
// src/lib/cn.ts
import { clsx, type ClassValue } from 'clsx'
import { twMerge } from 'tailwind-merge'

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}
```

```tsx
// Uso em componentes
import { cn } from '@/lib/cn'

<button className={cn('px-4 py-2 rounded-md', isActive && 'bg-blue-600 text-white')} />
```

### Convenções

```
✅ Usar classes Tailwind para todo estilo
✅ Usar cn() para classes condicionais ou que se combinam com props
✅ Extrair combinações longas para variáveis antes do JSX

❌ Nunca usar style={{ }} inline — exceto para valores genuinamente dinâmicos (ex: width em porcentagem calculada em runtime)
❌ Nunca misturar Tailwind com CSS externo para o mesmo elemento
❌ Nunca duplicar classes que conflitem (ex: `text-sm text-lg`) — use cn() que resolve o conflito
```

```tsx
// ❌ Style inline desnecessário
<div style={{ display: 'flex', gap: '8px', marginBottom: '16px' }}>

// ✅ Tailwind
<div className="flex gap-2 mb-4">

// ✅ Condicional com cn()
<div className={cn('flex gap-2', isOpen ? 'mb-4' : 'mb-0')}>

// ✅ Valor genuinamente dinâmico (calculado em JS)
<div className="relative" style={{ width: `${progress}%` }}>
```

### Variantes de componentes com `cn`

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

### Configuração (flat config, v9)

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

### Regras importantes

| Regra | Por quê |
|---|---|
| `no-explicit-any` como erro | Força tipagem correta — use `unknown` e narrowing |
| `no-unused-vars` como erro | Mantém o código limpo; prefixe com `_` o que é intencional |
| `react-hooks/rules-of-hooks` | Garante hooks chamados na ordem certa |
| `react-hooks/exhaustive-deps` | Evita bugs silenciosos em `useEffect` com deps faltando |
| `no-console` como warning | Logs de debug não devem ir para produção |

---

## TypeScript — Convenções

```typescript
// ✅ Tipos de domínio em types.ts de cada serviço ou feature
// ✅ Tipos globais (User, Pagination, ApiResponse) em src/types/index.ts
// ✅ Props de componentes definidas como interface no mesmo arquivo
// ❌ Nunca usar `any` — prefira `unknown` e narrowing

// Interface de props junto ao componente
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

## Checklist para nova feature

- [ ] Pasta `features/{feature}/` criada com `components/`, `hooks/`, `index.ts`
- [ ] Tipos de API em `services/{feature}/types.ts`
- [ ] queryKeys em `services/{feature}/keys.ts`
- [ ] Queries em `services/{feature}/queries.ts`
- [ ] Mutations em `services/{feature}/actions.ts`
- [ ] Se precisar de estado global: store em `stores/{feature}/` com os 4 arquivos
- [ ] Componentes com no máximo 100 linhas — separar lógica em hook e JSX em subcomponentes
- [ ] Estilo via classes Tailwind — sem `style={{}}` inline exceto valores dinâmicos calculados em JS
- [ ] Classes condicionais usando `cn()` de `@/lib/cn`
- [ ] Textos visíveis ao usuário usando `t()` do i18n
- [ ] Funções utilitárias novas em `helpers/` com teste unitário
- [ ] Exports públicos da feature via `index.ts`

## Checklist para novo componente reutilizável

- [ ] Vai ser usado em mais de uma feature? → `components/shared/`
- [ ] É um elemento base sem lógica de negócio? → `components/ui/`
- [ ] Props tipadas com `interface` no mesmo arquivo
- [ ] Sem chamada direta a store ou API se for `ui/`
- [ ] Sem lógica inline no JSX — pré-computar antes do `return`
- [ ] Variantes de aparência via `cn()` e objeto de classes, não `style={{}}`
- [ ] `npm run lint` passa sem erros antes de commitar
