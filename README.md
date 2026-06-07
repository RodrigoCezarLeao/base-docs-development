# C# API Template — Controller / Service / Repository + Dapper

Template de referência para APIs RESTful em .NET 8 com três camadas bem definidas, Dapper ORM e padrões modernos de C#.

---

## Estrutura do Projeto

```
CSharpApiTemplate.sln
├── src/
│   ├── CSharpApiTemplate.Api            # Camada de apresentação (Controllers, Middleware)
│   ├── CSharpApiTemplate.Application    # Camada de aplicação (Services, DTOs, Requests, Responses)
│   ├── CSharpApiTemplate.Domain         # Camada de domínio (Models)
│   └── CSharpApiTemplate.Infrastructure # Camada de infraestrutura (Repositories, Dapper, DB)
└── migrations/
    └── 001_CreateProductsTable.sql
```

---

## Camadas e Responsabilidades

### Domain
- **Models** — entidades que espelham as tabelas do banco. Sem lógica de negócio, sem anotações de framework.

### Application
| Pasta | Propósito |
|---|---|
| `DTOs/` | Objetos de saída imutáveis (`record`). O que a API devolve. |
| `Requests/` | Objetos de entrada com validações (`DataAnnotations`). O que a API recebe. |
| `Responses/` | Envelopes genéricos `ApiResponse<T>` e `PagedResponse<T>`. |
| `Interfaces/` | Contratos dos services. |
| `Services/` | Regras de negócio, mapeamento Model→DTO, orquestração. |

### Infrastructure
| Pasta | Propósito |
|---|---|
| `Data/` | `IDbConnectionFactory` + `DbConnectionFactory` (SQL Server via Dapper). |
| `Repositories/Base/` | `IBaseRepository<T,TKey>` + `BaseRepository<T,TKey>` com helpers de query e transação. |
| `Repositories/Interfaces/` | Contratos específicos por entidade. |
| `Repositories/` | Implementações com Dapper. |

### Api
| Pasta | Propósito |
|---|---|
| `Controllers/` | Rotas REST, mapeamento de status codes HTTP. |
| `Middleware/` | `ExceptionHandlingMiddleware` — captura exceções e retorna resposta padronizada. |

---

## Padrões Utilizados

### Envelope de Resposta Padrão

```json
// Sucesso
{ "success": true, "data": { ... }, "message": null, "errors": null }

// Erro de negócio (404, 400)
{ "success": false, "data": null, "message": "Product with id 5 not found.", "errors": null }

// Erro de validação (400)
{ "success": false, "data": null, "message": "...", "errors": ["Name is required"] }
```

### Status Codes REST

| Operação | Rota | Status de Sucesso |
|---|---|---|
| Listar | `GET /api/v1/products?page=1&pageSize=10` | `200 OK` |
| Buscar | `GET /api/v1/products/{id}` | `200 OK` / `404 Not Found` |
| Criar | `POST /api/v1/products` | `201 Created` |
| Atualizar | `PUT /api/v1/products/{id}` | `200 OK` / `404 Not Found` |
| Deletar (soft) | `DELETE /api/v1/products/{id}` | `200 OK` / `404 Not Found` |

### BaseRepository

`BaseRepository<T, TKey>` fornece dois helpers:

```csharp
// Query simples — abre/fecha conexão automaticamente
await QueryAsync(conn => conn.QueryAsync<Product>(sql, params));

// Query com transação — commit/rollback automático
await QueryInTransactionAsync((conn, tx) => conn.ExecuteAsync(sql, params, tx));
```

Repositórios concretos herdam e sobrescrevem os métodos CRUD padrão, mais adicionam queries específicas via `IProductRepository`.

---

## Como Usar o Template

### 1. Banco de Dados

Execute a migration no SQL Server:
```bash
sqlcmd -S localhost -d CSharpApiTemplateDb -i migrations/001_CreateProductsTable.sql
```

### 2. Connection String

Ajuste `src/CSharpApiTemplate.Api/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CSharpApiTemplateDb;User Id=sa;Password=sua_senha;TrustServerCertificate=True;"
  }
}
```

### 3. Executar

```bash
dotnet run --project src/CSharpApiTemplate.Api
# Swagger em: https://localhost:5001/swagger
```

---

## Adicionando uma Nova Entidade

1. **Domain** — criar `Models/MinhaEntidade.cs`
2. **Infrastructure** — criar `Repositories/Interfaces/IMinhaEntidadeRepository.cs` e `Repositories/MinhaEntidadeRepository.cs`
3. **Application** — criar `DTOs/MinhaEntidadeDto.cs`, `Requests/Create|UpdateMinhaEntidadeRequest.cs`, `Interfaces/IMinhaEntidadeService.cs`, `Services/MinhaEntidadeService.cs`
4. **Api** — criar `Controllers/MinhaEntidadesController.cs`
5. **DI** — registrar repositório em `Infrastructure/DependencyInjection.cs` e service em `Application/DependencyInjection.cs`
6. **Migration** — criar novo arquivo SQL em `migrations/`

---

## Dependências

| Pacote | Versão | Camada |
|---|---|---|
| `Dapper` | 2.1.28 | Infrastructure |
| `Microsoft.Data.SqlClient` | 5.2.1 | Infrastructure |
| `Swashbuckle.AspNetCore` | 6.6.2 | Api |
