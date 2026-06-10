# base-docs-development

Base repository for Claude Code-assisted development. Contains architecture guidelines and reference projects.

## Reference project: temperature

The `temperature-api` + `temperature-web` projects implement the full stack following the patterns in `guidelines/`. Run them to see the reference implementation in action.

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 22.13+](https://nodejs.org/) — required by pnpm 11
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

---

## Backend — temperature-api

```bash
cd projects/temperature-api
```

**1. Start the database**

```bash
docker compose up -d
```

Starts PostgreSQL 16 on port `5432`. Migrations run automatically on first API startup.

**2. Run the API**

```bash
dotnet run --project src/TemperatureApi.Api
```

API available at `http://localhost:5000`.  
Swagger UI at `http://localhost:5000/swagger`.

**3. Run the tests**

```bash
dotnet test
```

**To stop the database**

```bash
docker compose down
```

Stops and removes the container. Data is preserved in the Docker volume — `docker compose up -d` will resume from the same state next time.

To also delete the data volume:

```bash
docker compose down -v
```

---

## Frontend — temperature-web

```bash
cd projects/temperature-web
```

**1. Install dependencies**

```bash
pnpm install
```

**2. Configure the environment**

```bash
cp .env.example .env.local
```

The default value (`VITE_API_URL=http://localhost:5000`) works as-is if the API is running locally.

**3. Start the dev server**

```bash
pnpm dev
```

App available at `http://localhost:5173`.

**4. Run the tests**

```bash
pnpm test:run
```

---

## Running both together

Open two terminals:

| Terminal | Command |
|----------|---------|
| 1 | `cd projects/temperature-api && docker compose up -d && dotnet run --project src/TemperatureApi.Api` |
| 2 | `cd projects/temperature-web && pnpm dev` |

Then open `http://localhost:5173`.

---

## Repository structure

```
guidelines/
  csharp-api.md       → REST API patterns: C# .NET 8 + Dapper + PostgreSQL
  react-frontend.md   → Frontend patterns: React + TypeScript + Vite + Tailwind
  infra-devops.md     → Deployment: VPS provisioning, Docker, CI/CD, nginx, HTTPS

projects/
  temperature-api/    → Reference backend (simple CRUD, no auth)
  temperature-web/    → Reference frontend (listing, form, tests)
  docmap-api/         → Showcase (JWT, related resources, zip export)
  docmap-web/         → Showcase (React Flow, Zustand persist, side panel)

infra/                → VPS provisioning & deploy templates (see guidelines/infra-devops.md)
```

See `CLAUDE.md` for conventions and instructions for creating new projects from this base.
