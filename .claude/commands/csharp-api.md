# Skill: csharp-api

Cria uma API RESTful em C# .NET 8 com o padrão Controller → Service → Repository usando Dapper ORM.

## Como usar

Quando esta skill for invocada, pergunte ao usuário:

1. **Nome da entidade** (singular, PascalCase) — ex: `Order`, `Customer`, `Product`
2. **Propriedades da entidade** — nome e tipo de cada campo
3. **Nome do banco de dados** (connection string)

Com base nas respostas, gere todos os arquivos descritos abaixo.

---

## Estrutura de Projetos

Crie uma solution com 4 projetos:

```
{SolutionName}.sln
src/
├── {SolutionName}.Api             ← Controllers, Middleware, Program.cs
├── {SolutionName}.Application     ← Services, DTOs, Requests, Responses, Interfaces
├── {SolutionName}.Domain          ← Models
└── {SolutionName}.Infrastructure  ← Repositories, Dapper, DbConnectionFactory
migrations/
└── 001_Create{Entity}Table.sql
```

---

## Arquivos a Gerar

### 1. Domain — `src/{SolutionName}.Domain/Models/{Entity}.cs`

```csharp
namespace {SolutionName}.Domain.Models;

public class {Entity}
{
    public int Id { get; set; }
    // propriedades informadas pelo usuário
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

Regras:
- Sempre incluir `Id`, `IsActive`, `CreatedAt`, `UpdatedAt`
- Sem anotações de framework, sem lógica de negócio

---

### 2. Application — DTOs, Requests, Responses, Interfaces, Services

#### `DTOs/{Entity}Dto.cs`

```csharp
namespace {SolutionName}.Application.DTOs;

public record {Entity}Dto(
    int Id,
    // propriedades da entidade (sem IsActive interno se não fizer sentido expor)
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
```

Use `record` imutável. Nunca exponha campos de senha, tokens ou dados sensíveis.

#### `Requests/Create{Entity}Request.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace {SolutionName}.Application.Requests;

public record Create{Entity}Request(
    [Required][MaxLength(200)] string Nome,
    // demais campos com validações adequadas
);
```

#### `Requests/Update{Entity}Request.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace {SolutionName}.Application.Requests;

public record Update{Entity}Request(
    [Required][MaxLength(200)] string Nome,
    // demais campos
    bool IsActive
);
```

Regras para requests:
- Sempre `record` com `DataAnnotations`
- `[Required]` para campos obrigatórios
- `[MaxLength]` para strings
- `[Range]` para números

#### `Responses/ApiResponse.cs`

```csharp
namespace {SolutionName}.Application.Responses;

public record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public IEnumerable<string>? Errors { get; init; }

    private ApiResponse(bool success, T? data, string? message, IEnumerable<string>? errors)
    {
        Success = success;
        Data = data;
        Message = message;
        Errors = errors;
    }

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new(true, data, message, null);

    public static ApiResponse<T> Created(T data) =>
        new(true, data, "Resource created successfully.", null);

    public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null) =>
        new(false, default, message, errors);
}
```

#### `Responses/PagedResponse.cs`

```csharp
namespace {SolutionName}.Application.Responses;

public record PagedResponse<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize
)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
```

#### `Interfaces/I{Entity}Service.cs`

```csharp
using {SolutionName}.Application.DTOs;
using {SolutionName}.Application.Requests;
using {SolutionName}.Application.Responses;

namespace {SolutionName}.Application.Interfaces;

public interface I{Entity}Service
{
    Task<ApiResponse<{Entity}Dto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResponse<{Entity}Dto>>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ApiResponse<{Entity}Dto>> CreateAsync(Create{Entity}Request request, CancellationToken cancellationToken = default);
    Task<ApiResponse<{Entity}Dto>> UpdateAsync(int id, Update{Entity}Request request, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
```

#### `Services/{Entity}Service.cs`

```csharp
using {SolutionName}.Application.DTOs;
using {SolutionName}.Application.Interfaces;
using {SolutionName}.Application.Requests;
using {SolutionName}.Application.Responses;
using {SolutionName}.Domain.Models;
using {SolutionName}.Infrastructure.Repositories.Interfaces;

namespace {SolutionName}.Application.Services;

public class {Entity}Service(I{Entity}Repository repository) : I{Entity}Service
{
    public async Task<ApiResponse<{Entity}Dto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return ApiResponse<{Entity}Dto>.Fail($"{Entity} with id {id} not found.");

        return ApiResponse<{Entity}Dto>.Ok(MapToDto(entity));
    }

    public async Task<ApiResponse<PagedResponse<{Entity}Dto>>> GetAllAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await repository.GetPagedAsync(page, pageSize, cancellationToken);
        var paged = new PagedResponse<{Entity}Dto>(items.Select(MapToDto), totalCount, page, pageSize);
        return ApiResponse<PagedResponse<{Entity}Dto>>.Ok(paged);
    }

    public async Task<ApiResponse<{Entity}Dto>> CreateAsync(
        Create{Entity}Request request, CancellationToken cancellationToken = default)
    {
        var entity = new {Entity}
        {
            // mapear propriedades do request para a entidade
            IsActive = true
        };

        var id = await repository.CreateAsync(entity, cancellationToken);
        entity.Id = id;
        return ApiResponse<{Entity}Dto>.Created(MapToDto(entity));
    }

    public async Task<ApiResponse<{Entity}Dto>> UpdateAsync(
        int id, Update{Entity}Request request, CancellationToken cancellationToken = default)
    {
        var existing = await repository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return ApiResponse<{Entity}Dto>.Fail($"{Entity} with id {id} not found.");

        // mapear propriedades do request para a entidade existente
        existing.IsActive = request.IsActive;

        await repository.UpdateAsync(existing, cancellationToken);
        return ApiResponse<{Entity}Dto>.Ok(MapToDto(existing), "{Entity} updated successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await repository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return ApiResponse<bool>.Fail($"{Entity} with id {id} not found.");

        await repository.DeleteAsync(id, cancellationToken);
        return ApiResponse<bool>.Ok(true, "{Entity} deleted successfully.");
    }

    private static {Entity}Dto MapToDto({Entity} e) => new(
        e.Id,
        // demais propriedades na ordem do record DTO
        e.IsActive,
        e.CreatedAt,
        e.UpdatedAt
    );
}
```

#### `DependencyInjection.cs` (Application)

```csharp
using {SolutionName}.Application.Interfaces;
using {SolutionName}.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace {SolutionName}.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<I{Entity}Service, {Entity}Service>();
        return services;
    }
}
```

---

### 3. Infrastructure — Base, DbConnectionFactory, Repositories

#### `Data/IDbConnectionFactory.cs`

```csharp
using System.Data;

namespace {SolutionName}.Infrastructure.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
```

#### `Data/DbConnectionFactory.cs`

```csharp
using System.Data;
using Microsoft.Data.SqlClient;

namespace {SolutionName}.Infrastructure.Data;

public class DbConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public IDbConnection CreateConnection() => new SqlConnection(connectionString);
}
```

#### `Repositories/Base/IBaseRepository.cs`

```csharp
namespace {SolutionName}.Infrastructure.Repositories.Base;

public interface IBaseRepository<T, TKey> where T : class
{
    Task<T?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TKey> CreateAsync(T entity, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default);
}
```

#### `Repositories/Base/BaseRepository.cs`

```csharp
using System.Data;
using {SolutionName}.Infrastructure.Data;

namespace {SolutionName}.Infrastructure.Repositories.Base;

public abstract class BaseRepository<T, TKey>(IDbConnectionFactory connectionFactory)
    : IBaseRepository<T, TKey> where T : class
{
    protected readonly IDbConnectionFactory ConnectionFactory = connectionFactory;

    protected IDbConnection CreateConnection() => ConnectionFactory.CreateConnection();

    // Abre e fecha a conexão automaticamente para queries simples
    protected async Task<TResult> QueryAsync<TResult>(
        Func<IDbConnection, Task<TResult>> query,
        CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        return await query(connection);
    }

    // Abre conexão, executa em transação, faz commit ou rollback automaticamente
    protected async Task<TResult> QueryInTransactionAsync<TResult>(
        Func<IDbConnection, IDbTransaction, Task<TResult>> query,
        CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            var result = await query(connection, transaction);
            transaction.Commit();
            return result;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public abstract Task<T?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    public abstract Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    public abstract Task<TKey> CreateAsync(T entity, CancellationToken cancellationToken = default);
    public abstract Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default);
    public abstract Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default);
}
```

#### `Repositories/Interfaces/I{Entity}Repository.cs`

```csharp
using {SolutionName}.Domain.Models;
using {SolutionName}.Infrastructure.Repositories.Base;

namespace {SolutionName}.Infrastructure.Repositories.Interfaces;

public interface I{Entity}Repository : IBaseRepository<{Entity}, int>
{
    Task<(IEnumerable<{Entity}> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
```

#### `Repositories/{Entity}Repository.cs`

```csharp
using {SolutionName}.Domain.Models;
using {SolutionName}.Infrastructure.Data;
using {SolutionName}.Infrastructure.Repositories.Base;
using {SolutionName}.Infrastructure.Repositories.Interfaces;
using Dapper;

namespace {SolutionName}.Infrastructure.Repositories;

public class {Entity}Repository(IDbConnectionFactory connectionFactory)
    : BaseRepository<{Entity}, int>(connectionFactory), I{Entity}Repository
{
    public override async Task<{Entity}?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM {Entity}s WHERE Id = @Id AND IsActive = 1";
        return await QueryAsync(conn =>
            conn.QueryFirstOrDefaultAsync<{Entity}>(sql, new { Id = id }), cancellationToken);
    }

    public override async Task<IEnumerable<{Entity}>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM {Entity}s WHERE IsActive = 1 ORDER BY CreatedAt DESC";
        return await QueryAsync(conn => conn.QueryAsync<{Entity}>(sql), cancellationToken);
    }

    public async Task<(IEnumerable<{Entity}> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT * FROM {Entity}s WHERE IsActive = 1
            ORDER BY CreatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(*) FROM {Entity}s WHERE IsActive = 1;";

        return await QueryAsync(async conn =>
        {
            using var multi = await conn.QueryMultipleAsync(sql, new
            {
                Offset = (page - 1) * pageSize,
                PageSize = pageSize
            });
            var items = await multi.ReadAsync<{Entity}>();
            var totalCount = await multi.ReadFirstAsync<int>();
            return (items, totalCount);
        }, cancellationToken);
    }

    public override async Task<int> CreateAsync({Entity} entity, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO {Entity}s (/* colunas */, IsActive, CreatedAt)
            OUTPUT INSERTED.Id
            VALUES (/* @valores */, @IsActive, @CreatedAt)";

        entity.CreatedAt = DateTime.UtcNow;
        return await QueryAsync(conn =>
            conn.ExecuteScalarAsync<int>(sql, entity), cancellationToken);
    }

    public override async Task<bool> UpdateAsync({Entity} entity, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE {Entity}s
            SET /* campo = @campo, */, IsActive = @IsActive, UpdatedAt = @UpdatedAt
            WHERE Id = @Id";

        entity.UpdatedAt = DateTime.UtcNow;
        var affected = await QueryAsync(conn =>
            conn.ExecuteAsync(sql, entity), cancellationToken);
        return affected > 0;
    }

    public override async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        // Soft delete — nunca apaga fisicamente
        const string sql = "UPDATE {Entity}s SET IsActive = 0, UpdatedAt = @UpdatedAt WHERE Id = @Id";
        var affected = await QueryAsync(conn =>
            conn.ExecuteAsync(sql, new { Id = id, UpdatedAt = DateTime.UtcNow }), cancellationToken);
        return affected > 0;
    }
}
```

#### `DependencyInjection.cs` (Infrastructure)

```csharp
using {SolutionName}.Infrastructure.Data;
using {SolutionName}.Infrastructure.Repositories;
using {SolutionName}.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace {SolutionName}.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddSingleton<IDbConnectionFactory>(_ => new DbConnectionFactory(connectionString));
        services.AddScoped<I{Entity}Repository, {Entity}Repository>();

        return services;
    }
}
```

---

### 4. Api — Controller, Middleware, Program.cs

#### `Controllers/{Entity}sController.cs`

```csharp
using {SolutionName}.Application.Interfaces;
using {SolutionName}.Application.Requests;
using Microsoft.AspNetCore.Mvc;

namespace {SolutionName}.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class {Entity}sController(I{Entity}Service service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var response = await service.GetAllAsync(page, pageSize, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var response = await service.GetByIdAsync(id, cancellationToken);
        return response.Success ? Ok(response) : NotFound(response);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] Create{Entity}Request request,
        CancellationToken cancellationToken = default)
    {
        var response = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Data!.Id }, response);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] Update{Entity}Request request,
        CancellationToken cancellationToken = default)
    {
        var response = await service.UpdateAsync(id, request, cancellationToken);
        return response.Success ? Ok(response) : NotFound(response);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var response = await service.DeleteAsync(id, cancellationToken);
        return response.Success ? Ok(response) : NotFound(response);
    }
}
```

Regras de status codes:
- `GET` lista → `200 OK`
- `GET` por id, encontrado → `200 OK` / não encontrado → `404 Not Found`
- `POST` criação → `201 Created` com header `Location`
- `PUT` atualização → `200 OK` / não encontrado → `404 Not Found`
- `DELETE` (soft) → `200 OK` / não encontrado → `404 Not Found`
- Validação inválida (DataAnnotations) → `400 Bad Request` automático pelo `[ApiController]`
- Exceção não tratada → `500 Internal Server Error` via middleware

#### `Middleware/ExceptionHandlingMiddleware.cs`

```csharp
using System.Net;
using System.Text.Json;
using {SolutionName}.Application.Responses;

namespace {SolutionName}.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = exception switch
        {
            ArgumentException        => (int)HttpStatusCode.BadRequest,
            KeyNotFoundException     => (int)HttpStatusCode.NotFound,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            _                        => (int)HttpStatusCode.InternalServerError
        };

        var isServerError = context.Response.StatusCode == (int)HttpStatusCode.InternalServerError;
        var response = ApiResponse<object>.Fail(
            isServerError ? "An internal server error occurred." : exception.Message
        );

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
```

#### `Program.cs`

```csharp
using {SolutionName}.Api.Middleware;
using {SolutionName}.Application;
using {SolutionName}.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "{SolutionName} API", Version = "v1" });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

#### `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database={DatabaseName};Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

---

### 5. Migration — `migrations/001_Create{Entity}Table.sql`

```sql
CREATE TABLE {Entity}s (
    Id          INT IDENTITY(1,1)   NOT NULL PRIMARY KEY,
    -- colunas da entidade informadas pelo usuário
    IsActive    BIT                 NOT NULL DEFAULT 1,
    CreatedAt   DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt   DATETIME2           NULL
);

CREATE INDEX IX_{Entity}s_IsActive  ON {Entity}s (IsActive);
CREATE INDEX IX_{Entity}s_CreatedAt ON {Entity}s (CreatedAt DESC);
```

---

## .csproj — Dependências por Projeto

**Domain** — sem dependências externas.

**Application** — referencia Domain.

**Infrastructure** — pacotes NuGet:
```xml
<PackageReference Include="Dapper" Version="2.1.28" />
<PackageReference Include="Microsoft.Data.SqlClient" Version="5.2.1" />
<PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.2" />
```

**Api** — pacote NuGet:
```xml
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
```

---

## Regras Gerais

- C# 12 / .NET 8 — usar `record`, primary constructors, file-scoped namespaces, `ImplicitUsings`, `Nullable enable`
- Soft delete obrigatório — nunca `DELETE` físico, sempre `UPDATE IsActive = 0`
- `CancellationToken` em todos os métodos async
- Repositório não conhece Application; Service não conhece Infrastructure diretamente (só via interface)
- Toda resposta da API passa pelo envelope `ApiResponse<T>`
- Nomes de tabela no plural (`{Entity}s`), nomes de classe no singular (`{Entity}`)
