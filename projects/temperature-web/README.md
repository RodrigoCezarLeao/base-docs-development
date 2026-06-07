# temperature-web

Frontend de referência — React 18 · TypeScript · Vite · React Query · Zustand · Tailwind CSS v4 · i18n

## Requisitos

- Node.js >= 18
- npm >= 9

## Início rápido

```bash
npm install
npm run dev       # http://localhost:5173
npm run lint      # ESLint
npm test          # Vitest
```

A API backend deve estar rodando em `http://localhost:5000` (configurável em `.env.development`).

## Compatibilidade de versões — importante

**Vite e @tailwindcss/vite têm um acoplamento de versão.**

| Vite | @tailwindcss/vite 4.x | Status |
|---|---|---|
| 5.x | ✅ suportado | versão usada neste projeto |
| 6.x | ✅ suportado | pode ser usado sem mudanças |
| 7.x | ❌ não suportado | aguardando fix upstream |
| 8.x | ❌ não suportado | aguardando fix upstream |

> **Não atualize o Vite para 7+ sem verificar o changelog de `@tailwindcss/vite`.**
> Fonte: [issue vitejs/vite#20284](https://github.com/vitejs/vite/issues/20284) e [tailwindlabs/tailwindcss#19789](https://github.com/tailwindlabs/tailwindcss/issues/19789)

Se precisar de Vite 7+ agora, a alternativa é substituir `@tailwindcss/vite` por `@tailwindcss/postcss`:

```ts
// vite.config.ts — fallback sem @tailwindcss/vite
export default defineConfig({
  plugins: [react()],
  css: { postcss: './postcss.config.js' },
})
```

```js
// postcss.config.js
export default {
  plugins: { '@tailwindcss/postcss': {} }
}
```

## Estrutura

```
src/
├── components/ui/          → Button, Spinner (sem lógica de negócio)
├── components/shared/      → PageHeader (reutilizável entre features)
├── features/
│   └── temperature-list/   → componentes, hook e exports da feature
├── helpers/                → funções puras + testes unitários
├── i18n/                   → locales pt-BR e en
├── lib/                    → queryClient, api (axios), cn (tailwind-merge)
├── pages/home/             → página raiz
├── services/temperatures/  → types, keys, queries, actions (React Query)
├── stores/temperature/     → types, store, selectors, hooks (Zustand)
└── types/                  → tipos globais (ApiResponse, PagedResponse)
```
