# Guideline — API REST em C# com Controller / Service / Repository + Dapper

## Propósito

Este documento define o padrão adotado para construção de APIs RESTful em C# .NET 8. O objetivo é garantir consistência entre projetos: mesma estrutura de pastas, mesmas convenções de nomenclatura, mesmo envelope de resposta e mesmo comportamento HTTP.

Qualquer desenvolvedor (ou o Claude) deve conseguir criar uma nova entidade seguindo este guia sem precisar inventar estrutura.

---

## Estrutura de Projetos

Toda API segue uma solution com **4 projetos separados por responsabilidade**:

```
{NomeDaSolution}.sln
src/
├── {Nome}.Api             → Entrada HTTP: controllers, middleware, Program.cs
├── {Nome}.Application     → Regras de negócio: services, DTOs, requests, responses
├── {Nome}.Domain          → Entidades de domínio: models puros
└── {Nome}.Infrastructure  → Acesso a dados: Dapper, repositórios, fábrica de conexão
migrations/
└── 001_Create{Entidade}Table.sql
```

**Direção das dependências** (nunca invertida):

```
Api → Application → Domain
         ↑
  Infrastructure → Domain
```

Infrastructure conhece Application (usa suas interfaces), mas Application nunca referencia Infrastructure diretamente — só via contrato.

---

## Camada Domain

Contém apenas as **entidades que espelham as tabelas do banco**. Sem lógica de negócio, sem atributos de framework, sem dependências externas.

Todo model deve incluir os campos de controle obrigatórios:

```csharp
namespace {Nome}.Domain.Models;

public class Pedido
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public bool IsActive { get; set; } = true;   // soft delete
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

---

## Camada Application

### DTOs

Objeto de **saída** da API. Sempre `record` imutável. Nunca exponha campos sensíveis (senha, tokens, dados internos).

```csharp
public record PedidoDto(
    int Id,
    string Numero,
    decimal Total,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
```

### Requests

Objeto de **entrada** da API. Sempre `record` com `DataAnnotations` para validação.

```csharp
public record CreatePedidoRequest(
    [Required][MaxLength(50)] string Numero,
    [Range(0.01, double.MaxValue)] decimal Total
);

public record UpdatePedidoRequest(
    [Required][MaxLength(50)] string Numero,
    [Range(0.01, double.MaxValue)] decimal Total,
    bool IsActive
);
```

### Envelope de Resposta

**Toda** resposta da API é envolvida em `ApiResponse<T>`. Nunca retorne o objeto diretamente.

```csharp
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

Formato de sucesso:
```json
{ "success": true, "data": { ... }, "message": null, "errors": null }
```

Formato de erro:
```json
{ "success": false, "data": null, "message": "Pedido com id 5 não encontrado.", "errors": null }
```

### Paginação

Sempre paginar listagens. Use `PagedResponse<T>`:

```csharp
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

### Service

O service orquestra a lógica de negócio. Ele:
- Chama o repositório via interface
- Valida regras de negócio (não validação de formato — isso é responsabilidade do request)
- Mapeia Model → DTO
- Retorna `ApiResponse<T>`

```csharp
public class PedidoService(IPedidoRepository repository) : IPedidoService
{
    public async Task<ApiResponse<PedidoDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var pedido = await repository.GetByIdAsync(id, cancellationToken);
        if (pedido is null)
            return ApiResponse<PedidoDto>.Fail($"Pedido com id {id} não encontrado.");

        return ApiResponse<PedidoDto>.Ok(MapToDto(pedido));
    }

    // ... demais métodos

    private static PedidoDto MapToDto(Pedido p) =>
        new(p.Id, p.Numero, p.Total, p.IsActive, p.CreatedAt, p.UpdatedAt);
}
```

O service **não** lança exceção quando um registro não é encontrado — retorna `Fail()`. Exceções são para erros inesperados, não para fluxo de negócio.

---

## Camada Infrastructure

### Fábrica de Conexão

Centraliza a criação da conexão com o banco. Registrada como `Singleton` no DI.

```csharp
public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

public class DbConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public IDbConnection CreateConnection() => new SqlConnection(connectionString);
}
```

### BaseRepository

Todo repositório herda de `BaseRepository<T, TKey>`, que oferece dois helpers:

```csharp
public abstract class BaseRepository<T, TKey>(IDbConnectionFactory connectionFactory)
    : IBaseRepository<T, TKey> where T : class
{
    // Abre e fecha a conexão automaticamente
    protected async Task<TResult> QueryAsync<TResult>(
        Func<IDbConnection, Task<TResult>> query,
        CancellationToken cancellationToken = default)
    {
        using var connection = ConnectionFactory.CreateConnection();
        return await query(connection);
    }

    // Abre conexão, executa em transação, commit ou rollback automático
    protected async Task<TResult> QueryInTransactionAsync<TResult>(
        Func<IDbConnection, IDbTransaction, Task<TResult>> query,
        CancellationToken cancellationToken = default)
    {
        using var connection = ConnectionFactory.CreateConnection();
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
}
```

`IBaseRepository<T, TKey>` define o contrato CRUD:

```csharp
public interface IBaseRepository<T, TKey> where T : class
{
    Task<T?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TKey> CreateAsync(T entity, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default);
}
```

### Repositório Concreto

Cada entidade tem sua própria interface (para queries específicas) e implementação:

```csharp
public interface IPedidoRepository : IBaseRepository<Pedido, int>
{
    Task<(IEnumerable<Pedido> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken = default);
}

public class PedidoRepository(IDbConnectionFactory connectionFactory)
    : BaseRepository<Pedido, int>(connectionFactory), IPedidoRepository
{
    public override async Task<Pedido?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM Pedidos WHERE Id = @Id AND IsActive = 1";
        return await QueryAsync(conn =>
            conn.QueryFirstOrDefaultAsync<Pedido>(sql, new { Id = id }), cancellationToken);
    }

    public async Task<(IEnumerable<Pedido> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        // QueryMultiple faz as duas queries em uma única ida ao banco
        const string sql = @"
            SELECT * FROM Pedidos WHERE IsActive = 1
            ORDER BY CreatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(*) FROM Pedidos WHERE IsActive = 1;";

        return await QueryAsync(async conn =>
        {
            using var multi = await conn.QueryMultipleAsync(sql, new
            {
                Offset = (page - 1) * pageSize,
                PageSize = pageSize
            });
            var items = await multi.ReadAsync<Pedido>();
            var totalCount = await multi.ReadFirstAsync<int>();
            return (items, totalCount);
        }, cancellationToken);
    }

    public override async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        // Sempre soft delete — nunca DELETE físico
        const string sql = "UPDATE Pedidos SET IsActive = 0, UpdatedAt = @UpdatedAt WHERE Id = @Id";
        var affected = await QueryAsync(conn =>
            conn.ExecuteAsync(sql, new { Id = id, UpdatedAt = DateTime.UtcNow }), cancellationToken);
        return affected > 0;
    }
}
```

---

## Camada Api

### Controller

Responsabilidade única: receber a requisição HTTP, chamar o service e devolver o status code correto.

```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class PedidosController(IPedidoService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var response = await service.GetAllAsync(page, pageSize, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var response = await service.GetByIdAsync(id, cancellationToken);
        return response.Success ? Ok(response) : NotFound(response);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreatePedidoRequest request, CancellationToken cancellationToken = default)
    {
        var response = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Data!.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id, [FromBody] UpdatePedidoRequest request, CancellationToken cancellationToken = default)
    {
        var response = await service.UpdateAsync(id, request, cancellationToken);
        return response.Success ? Ok(response) : NotFound(response);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var response = await service.DeleteAsync(id, cancellationToken);
        return response.Success ? Ok(response) : NotFound(response);
    }
}
```

### Status Codes

| Operação | Rota | Sucesso | Não encontrado | Inválido |
|---|---|---|---|---|
| Listar | `GET /api/v1/pedidos` | `200` | — | — |
| Buscar | `GET /api/v1/pedidos/{id}` | `200` | `404` | — |
| Criar | `POST /api/v1/pedidos` | `201` | — | `400` |
| Atualizar | `PUT /api/v1/pedidos/{id}` | `200` | `404` | `400` |
| Deletar | `DELETE /api/v1/pedidos/{id}` | `200` | `404` | — |

O `400` por validação de request é automático via `[ApiController]` + `DataAnnotations`.

### Middleware de Exceção

Captura exceções não tratadas e retorna `ApiResponse<T>` padronizado:

```csharp
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
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
            ArgumentException           => 400,
            KeyNotFoundException        => 404,
            UnauthorizedAccessException => 401,
            _                           => 500
        };

        var isServerError = context.Response.StatusCode == 500;
        var response = ApiResponse<object>.Fail(
            isServerError ? "An internal server error occurred." : exception.Message);

        await context.Response.WriteAsync(JsonSerializer.Serialize(response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
```

### Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();       // extension method em Application/DependencyInjection.cs
builder.Services.AddInfrastructure(builder.Configuration); // extension method em Infrastructure/DependencyInjection.cs

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

---

## Injeção de Dependência

Cada camada expõe um extension method de registro. O `Program.cs` chama apenas esses dois métodos.

```csharp
// Application/DependencyInjection.cs
public static IServiceCollection AddApplication(this IServiceCollection services)
{
    services.AddScoped<IPedidoService, PedidoService>();
    return services;
}

// Infrastructure/DependencyInjection.cs
public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
{
    var cs = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    services.AddSingleton<IDbConnectionFactory>(_ => new DbConnectionFactory(cs));
    services.AddScoped<IPedidoRepository, PedidoRepository>();
    return services;
}
```

---

## Migration SQL

```sql
CREATE TABLE Pedidos (
    Id        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Numero    NVARCHAR(50)      NOT NULL,
    Total     DECIMAL(18,2)     NOT NULL,
    IsActive  BIT               NOT NULL DEFAULT 1,
    CreatedAt DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2         NULL
);

CREATE INDEX IX_Pedidos_IsActive  ON Pedidos (IsActive);
CREATE INDEX IX_Pedidos_CreatedAt ON Pedidos (CreatedAt DESC);
```

---

## Convenções

| Regra | Detalhe |
|---|---|
| Soft delete | Sempre `UPDATE IsActive = 0`. Nunca `DELETE` físico. |
| `CancellationToken` | Obrigatório em todos os métodos `async`. |
| Nomes de tabela | Plural (`Pedidos`). Nomes de classe no singular (`Pedido`). |
| DTOs e Requests | Sempre `record` imutável. |
| Versão da rota | Sempre prefixar com `api/v1/`. |
| Dependências NuGet | Dapper `2.1.28`, Microsoft.Data.SqlClient `5.2.1`, Swashbuckle.AspNetCore `6.6.2`. |
| Target framework | .NET 8 com `Nullable enable` e `ImplicitUsings enable`. |

---

## Checklist para nova entidade

- [ ] `Domain/Models/{Entidade}.cs` com `Id`, `IsActive`, `CreatedAt`, `UpdatedAt`
- [ ] `Application/DTOs/{Entidade}Dto.cs`
- [ ] `Application/Requests/Create{Entidade}Request.cs` e `Update{Entidade}Request.cs`
- [ ] `Application/Interfaces/I{Entidade}Service.cs`
- [ ] `Application/Services/{Entidade}Service.cs`
- [ ] `Infrastructure/Repositories/Interfaces/I{Entidade}Repository.cs`
- [ ] `Infrastructure/Repositories/{Entidade}Repository.cs`
- [ ] `Api/Controllers/{Entidade}sController.cs`
- [ ] Registrar no DI de Application e Infrastructure
- [ ] `migrations/NNN_Create{Entidade}Table.sql`
