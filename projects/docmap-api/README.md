# DocMap API

Backend for the DocMap visual documentation system. Built with .NET 8, Dapper, and PostgreSQL.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/) and Docker Compose

## Local Setup

### 1. Start the database

```bash
docker compose up -d
```

This starts PostgreSQL 16 on port **5433** (separate from temperature-api which uses 5432).

### 2. Run the API

```bash
cd src/DocMap.Api
dotnet run
```

The API starts at `http://localhost:5172`. Database migrations run automatically on startup.

### 3. Run tests

```bash
cd tests/DocMap.Tests
dotnet test
```

## Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/v1/auth/register` | — | Register new user |
| POST | `/api/v1/auth/login` | — | Login, returns JWT |
| GET | `/api/v1/projects` | Bearer | List projects |
| POST | `/api/v1/projects` | Bearer | Create project |
| GET | `/api/v1/projects/{id}` | Bearer | Get project |
| DELETE | `/api/v1/projects/{id}` | Bearer | Delete project |
| GET | `/api/v1/projects/{id}/documents` | Bearer | List documents |
| POST | `/api/v1/projects/{id}/documents` | Bearer | Create document |
| GET | `/api/v1/projects/{id}/documents/{docId}` | Bearer | Get document |
| PUT | `/api/v1/projects/{id}/documents/{docId}` | Bearer | Update document content |
| PATCH | `/api/v1/projects/{id}/documents/{docId}/position` | Bearer | Update canvas position |
| DELETE | `/api/v1/projects/{id}/documents/{docId}` | Bearer | Delete document |
| GET | `/api/v1/projects/{id}/connections` | Bearer | List connections |
| POST | `/api/v1/projects/{id}/connections` | Bearer | Create connection (auto-adds reference) |
| DELETE | `/api/v1/projects/{id}/connections/{connId}` | Bearer | Delete connection (removes reference) |
| GET | `/api/v1/projects/{id}/export` | Bearer | Export project as zip |
| GET | `/ping` | — | Liveness check |
| GET | `/health` | — | Readiness check |

Swagger UI: `http://localhost:5172/swagger`

## Environment

Configuration lives in `src/DocMap.Api/appsettings.json`. Override locally with `appsettings.Development.json` (git-ignored):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=docmap_db;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Secret": "your-secret-key-min-32-chars"
  },
  "Cors": {
    "AllowedOrigins": "http://localhost:5174"
  }
}
```

## Architecture

```
src/
  DocMap.Api/           # HTTP layer — controllers, middleware, Program.cs
  DocMap.Application/   # Use-case services (AuthService, DocumentService, ...)
  DocMap.Domain/        # Entities and repository interfaces
  DocMap.Infrastructure/ # Dapper repositories, DbUp migrations, JWT
tests/
  DocMap.Tests/         # Integration tests using WebApplicationFactory
```
