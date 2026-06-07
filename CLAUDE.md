# base-docs-development

Repositório base para desenvolvimento assistido por Claude Code. Contém guidelines de arquitetura e projetos de referência implementando esses padrões.

## O que é este repositório

Serve como ponto de partida para novos projetos. Quando iniciar uma sessão:

1. Leia `guidelines/` — são a fonte de verdade dos padrões adotados
2. Consulte `projects/temperature-*` como implementação de referência mínima
3. Consulte `projects/docmap-*` como showcase com JWT, React Flow e funcionalidades mais avançadas

## Estrutura

```
guidelines/
  csharp-api.md         → API REST C# .NET 8 + Dapper + PostgreSQL + DbUp + testes
  react-frontend.md     → React + TypeScript + Vite 5 + Tailwind v4 + React Query + Zustand + Vitest

projects/
  temperature-api/      → Backend de referência — CRUD simples, sem auth
  temperature-web/      → Frontend de referência — listagem, formulário, testes
  docmap-api/           → Showcase — JWT, múltiplos recursos relacionados, export zip
  docmap-web/           → Showcase — React Flow canvas, Zustand persist, side panel editor
```

## Criar novo projeto a partir deste base

**Backend:** copie `temperature-api/` para um novo diretório e rode o script de bootstrap:

```bash
cp -r projects/temperature-api projects/meu-projeto-api
cd projects/meu-projeto-api
bash rename.sh MeuProjeto          # ex: OrdersApi, ProductCatalog
```

O script substitui namespaces, nome do banco e volume Docker em todos os arquivos e renomeia pastas e `.sln` automaticamente. A entidade de exemplo (`TemperatureReading`) é mantida como referência — adicione suas entidades seguindo o mesmo padrão e remova-a quando não precisar mais. Para JWT, siga a seção correspondente em `guidelines/csharp-api.md`.

**Frontend:** copie `temperature-web/` e ajuste `package.json`. Certifique-se de que `"type":"module"` e `"types":["vite/client"]` estão presentes — ambos são obrigatórios.

## Portas locais (convenção)

| Projeto | Serviço | Porta |
|---------|---------|-------|
| temperature-api | API | 5000 |
| temperature-web | Dev server | 5173 |
| temperature-api | PostgreSQL | 5432 |
| docmap-api | API | 5172 |
| docmap-web | Dev server | 5174 |
| docmap-api | PostgreSQL | 5433 |

Use portas diferentes para evitar conflito quando rodar múltiplos projetos ao mesmo tempo.

## Stack

| Camada | Stack |
|--------|-------|
| Backend | .NET 8, Dapper, PostgreSQL 16, DbUp, Npgsql |
| Backend testes | xUnit, FluentAssertions, NSubstitute, WebApplicationFactory |
| Frontend | React 18, TypeScript 5, Vite 5, Tailwind v4 |
| Frontend estado | TanStack Query 5 (servidor), Zustand 4 (cliente) |
| Frontend testes | Vitest 2.x, Testing Library, jsdom |
| Infra local | Docker Compose (PostgreSQL) |

## Convenções críticas (resumo)

**Backend:**
- `DefaultTypeMap.MatchNamesWithUnderscores = true` — snake_case no banco, PascalCase no C#
- Toda resposta dentro de `ApiResponse<T>` — nunca retorne o objeto diretamente
- Migrações como `EmbeddedResource` numeradas (`001_*.sql`) — nunca alterar script já executado
- `public partial class Program {}` no final de `Program.cs` — obrigatório para `WebApplicationFactory`
- Testes de integração substituem repositórios e `IMigrationRunner` por mocks — sem banco real no CI

**Frontend:**
- `"type":"module"` no `package.json` — obrigatório para `@tailwindcss/vite`
- `"types":["vite/client"]` no `tsconfig.json` — obrigatório para `import.meta.env`
- `api.ts` exporta `ApiInstance` typed — sem casts nos serviços
- `mockReset()` no `beforeEach` (não `mockClear`) — limpa implementações também
- `mockRejectedValueOnce()` (não `mockRejectedValue`) — Vitest 2.x detecta o variant permanente como unhandled rejection
- `return await` em funções async de serviço — propagação correta de erros

## Comandos rápidos

```bash
# Backend (em projects/{nome}-api/)
docker compose up -d          # inicia PostgreSQL
dotnet run --project src/{Nome}.Api
dotnet test

# Frontend (em projects/{nome}-web/)
npm install
cp .env.example .env.local    # edite com a URL da API
npm run dev
npm run test:run
```
