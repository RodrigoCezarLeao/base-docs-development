# base-docs-development

Base repository for Claude Code-assisted development. Contains architecture guidelines and reference projects implementing those patterns.

## What this repository is

Serves as a starting point for new projects. When starting a session:

1. Read `guidelines/` — they are the source of truth for adopted patterns
2. Use `projects/temperature-*` as the minimal reference implementation
3. Use `projects/docmap-*` as a showcase with JWT, React Flow, and more advanced features

## Structure

```
guidelines/
  csharp-api.md         → REST API C# .NET 8 + Dapper + PostgreSQL + DbUp + tests
  react-frontend.md     → React + TypeScript + Vite 5 + Tailwind v4 + React Query + Zustand + Vitest

projects/
  temperature-api/      → Reference backend — simple CRUD, no auth
  temperature-web/      → Reference frontend — listing, form, tests
  docmap-api/           → Showcase — JWT, multiple related resources, zip export
  docmap-web/           → Showcase — React Flow canvas, Zustand persist, side panel editor
```

## Creating a new project from this base

**Backend:** copy `temperature-api/` to a new directory and run the bootstrap script:

```bash
cp -r projects/temperature-api projects/my-project-api
cd projects/my-project-api
bash rename.sh MyProject          # e.g.: OrdersApi, ProductCatalog
```

The script replaces namespaces, database name, and Docker volume across all files and automatically renames folders and `.sln`. The example entity (`TemperatureReading`) is kept as a reference — add your entities following the same pattern and remove it when no longer needed. For JWT, follow the corresponding section in `guidelines/csharp-api.md`.

**Frontend:** copy `temperature-web/` and adjust `package.json`. Make sure `"type":"module"` and `"types":["vite/client"]` are present — both are required.

## Local ports (convention)

| Project | Service | Port |
|---------|---------|------|
| temperature-api | API | 5000 |
| temperature-web | Dev server | 5173 |
| temperature-api | PostgreSQL | 5432 |
| docmap-api | API | 5172 |
| docmap-web | Dev server | 5174 |
| docmap-api | PostgreSQL | 5433 |

Use different ports to avoid conflicts when running multiple projects at the same time.

## Stack

| Layer | Stack |
|-------|-------|
| Backend | .NET 8, Dapper, PostgreSQL 16, DbUp, Npgsql |
| Backend tests | xUnit, FluentAssertions, NSubstitute, WebApplicationFactory |
| Frontend | React 18, TypeScript 5, Vite 5, Tailwind v4 |
| Frontend state | TanStack Query 5 (server), Zustand 4 (client) |
| Frontend tests | Vitest 2.x, Testing Library, jsdom |
| Local infra | Docker Compose (PostgreSQL) |

## Language

- **Conversations:** always in Portuguese — all responses, explanations, and questions to the user
- **Code, documentation, and git:** always in English — source files, comments, commit messages, PR descriptions, guidelines

## Critical conventions (summary)

**Backend:**
- `DefaultTypeMap.MatchNamesWithUnderscores = true` — snake_case in the database, PascalCase in C#
- Every response wrapped in `ApiResponse<T>` — never return the object directly
- Migrations as numbered `EmbeddedResource` (`001_*.sql`) — never alter an already-executed script
- `public partial class Program {}` at the end of `Program.cs` — required for `WebApplicationFactory`
- Integration tests replace repositories and `IMigrationRunner` with mocks — no real database in CI

**Frontend:**
- `"type":"module"` in `package.json` — required for `@tailwindcss/vite`
- `"types":["vite/client"]` in `tsconfig.json` — required for `import.meta.env`
- `api.ts` exports typed `ApiInstance` — no casts in services
- `mockReset()` in `beforeEach` (not `mockClear`) — also clears implementations
- `mockRejectedValueOnce()` (not `mockRejectedValue`) — Vitest 2.x detects the permanent variant as unhandled rejection
- `return await` in async service functions — correct error propagation

## Quick commands

```bash
# Backend (in projects/{name}-api/)
docker compose up -d          # starts PostgreSQL
dotnet run --project src/{Name}.Api
dotnet test

# Frontend (in projects/{name}-web/)
pnpm install
cp .env.example .env.local    # edit with the API URL
pnpm dev
pnpm test:run
```
