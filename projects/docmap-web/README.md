# DocMap Web

Frontend for the DocMap visual documentation system. Built with React 18, TypeScript, Vite 5, Tailwind CSS v4, and React Flow.

## Prerequisites

- [Node.js 18+](https://nodejs.org/)
- DocMap API running locally (see `../docmap-api/README.md`)

## Local Setup

### 1. Install dependencies

```bash
pnpm install
```

### 2. Configure the API URL

Create a `.env.local` file (git-ignored):

```env
VITE_API_URL=http://localhost:5172
```

### 3. Start the dev server

```bash
pnpm dev
```

Opens at `http://localhost:5174`.

## Scripts

| Command | Description |
|---------|-------------|
| `pnpm dev` | Start dev server with HMR |
| `pnpm build` | Type-check and build for production |
| `pnpm preview` | Preview production build |
| `pnpm lint` | Lint with ESLint |
| `pnpm lint:fix` | Auto-fix lint errors |
| `pnpm test` | Run tests in watch mode |
| `pnpm test:run` | Run tests once (CI mode) |
| `pnpm test:coverage` | Generate coverage report |

## Project Structure

```
src/
  components/ui/       # Shared design system components
  features/            # Feature-scoped components
    canvas/            # React Flow canvas, DocumentNode, DocumentSidePanel, CanvasToolbar
  lib/
    api.ts             # Typed axios instance
  pages/               # Route-level components (Login, Register, Projects, Canvas)
  router/              # React Router configuration
  services/            # API service layer
    auth/              # Login / register mutations
    projects/          # Project CRUD queries + mutations
    documents/         # Document CRUD queries + mutations
    connections/       # Connection CRUD queries + mutations
  stores/
    auth/              # Zustand auth store (persisted to localStorage)
    canvas/            # Zustand canvas store (selected document)
  test/
    setup.ts           # jest-dom matchers
    utils.tsx          # renderWithProviders helper
  types/               # Shared TypeScript types
```

## Testing

Tests use Vitest + Testing Library + jsdom.

```bash
pnpm test:run
```

**Key conventions:**
- Mock `@/lib/api` with `vi.mock('@/lib/api', () => ({ api: { get: vi.fn() } }))`
- Use `mockResolvedValueOnce` / `mockRejectedValueOnce` (not the persistent variants) to avoid orphaned rejected promises in Vitest 2.x
- Use `renderWithProviders` from `src/test/utils.tsx` for components that need React Query

## Canvas Features

- **Drag & drop** documents to reposition them (positions auto-saved)
- **Click a node** to open the side panel editor
- **Draw an arrow** between two nodes to create a connection — the source document automatically gets a `## Referências` section added with a link to the target
- **Delete a connection** to remove that reference line from the markdown
- **Create Document** button in the toolbar spawns a new node
- **Export** button downloads the whole project as a `.zip` with the markdown files organized by their `filePath`
