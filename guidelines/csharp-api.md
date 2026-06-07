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
└── {Nome}.Infrastructure  → Acesso a dados: Dapper, repositórios, fábrica de conexão, migrações
tests/
└── {Nome}.Tests           → Testes unitários (Unit/) e de integração (Integration/)
docker-compose.yml         → Banco PostgreSQL para desenvolvimento local
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

// PostgreSQL (padrão)
public class DbConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public IDbConnection CreateConnection() => new NpgsqlConnection(connectionString);
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
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Minha API",
        Version = "v1",
        Description = "Descrição da API."
    });
});

builder.Services.AddHealthChecks();
// Para verificar conectividade com o banco, adicione:
// builder.Services.AddHealthChecks().AddNpgsql(connectionString);
// (requer NuGet: AspNetCore.HealthChecks.Npgsql)

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.Services.GetRequiredService<IMigrationRunner>().Run();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Liveness — sem dependências, sempre responde
app.MapGet("/ping", () => Results.Ok(new { status = "ok" }))
   .ExcludeFromDescription();

// Readiness — verifica saúde das dependências registradas
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name    = e.Key,
                status  = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        });
        await context.Response.WriteAsync(result);
    }
});

app.Run();

// Expõe Program para WebApplicationFactory nos testes de integração
public partial class Program { }
```

### Swagger

O Swagger é configurado via `Swashbuckle.AspNetCore` e exposto apenas em `Development`.

```csharp
// Registro
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title  = "Minha API",
        Version = "v1",
        Description = "Descrição da API."
    });
});

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // UI em /swagger
}
```

Para que os endpoints apareçam corretamente no Swagger, os controllers devem declarar `[Produces("application/json")]` e os métodos devem usar `[ProducesResponseType]`:

```csharp
[Produces("application/json")]
public class PedidosController : ControllerBase
{
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id) { ... }
}
```

UI disponível em: `http://localhost:{porta}/swagger`

### Health Checks

Toda API deve expor dois endpoints de verificação de saúde:

| Endpoint | Finalidade | Verifica banco? |
|---|---|---|
| `GET /ping` | Liveness — a aplicação está viva? | Não |
| `GET /health` | Readiness — as dependências estão disponíveis? | Sim (se configurado) |

`/ping` é um endpoint minimal API simples, sem overhead de dependências. É usado por orquestradores (Kubernetes, Railway, Render) para saber se o processo está rodando.

`/health` usa o sistema de health checks do ASP.NET Core. Em produção, adicione verificação de banco:

```bash
dotnet add package AspNetCore.HealthChecks.Npgsql
```

```csharp
builder.Services.AddHealthChecks()
    .AddNpgsql(connectionString, name: "postgresql");
```

Resposta de `/health` (JSON customizado):
```json
{
  "status": "Healthy",
  "checks": [
    { "name": "postgresql", "status": "Healthy", "description": null }
  ]
}
```

Quando uma dependência falha, `/health` retorna `503 Service Unavailable` com `"status": "Unhealthy"`.

---

## Autenticação JWT (opcional)

Quando a API precisa de autenticação, use **JWT Bearer com HS256**. Adicione ao `Api.csproj`:

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.8" />
```

`appsettings.json`:
```json
{
  "Jwt": { "Secret": "seu-secret-min-32-chars-troque-em-producao!" }
}
```

`Program.cs` — registre o middleware de autenticação e Swagger com suporte a Bearer:

```csharp
// Registro
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("JWT secret not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
        };
    });

builder.Services.AddAuthorization();

// Swagger com Bearer
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http,
        Scheme = "Bearer", BearerFormat = "JWT",
        In = ParameterLocation.Header,
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference
                { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// Middleware (ordem importa — Authentication antes de Authorization)
app.UseAuthentication();
app.UseAuthorization();
```

**Geração do token** no `AuthService`:

```csharp
public string GenerateToken(User user)
{
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        claims: [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
        ],
        expires: DateTime.UtcNow.AddDays(7),
        signingCredentials: creds
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

**Nos controllers** — proteja com `[Authorize]` e leia o ID do usuário via `ClaimTypes.NameIdentifier`:

```csharp
[Authorize]
[HttpGet]
public async Task<IActionResult> GetAll()
{
    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var response = await _service.GetAllAsync(userId);
    return Ok(response);
}
```

**Senhas** — nunca armazene texto plano. Use BCrypt:

```xml
<PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
```

```csharp
// Hash ao criar usuário
string hash = BCrypt.Net.BCrypt.HashPassword(plainPassword);

// Verificação no login
bool isValid = BCrypt.Net.BCrypt.Verify(plainPassword, storedHash);
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

    // Mapeia snake_case do PostgreSQL para PascalCase do C# automaticamente
    DefaultTypeMap.MatchNamesWithUnderscores = true;

    services.AddSingleton<IDbConnectionFactory>(_ => new DbConnectionFactory(cs));
    services.AddSingleton<IMigrationRunner>(_ => new DbUpMigrationRunner(cs));
    services.AddScoped<IPedidoRepository, PedidoRepository>();
    return services;
}
```

---

## Banco de Dados — PostgreSQL

O banco padrão para desenvolvimento local é **PostgreSQL 16** rodando via Docker.

### docker-compose.yml

```yaml
services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: minha_db
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres -d minha_db"]
      interval: 5s
      timeout: 5s
      retries: 5
      start_period: 10s

volumes:
  postgres_data:
```

### Connection string (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=minha_db;Username=postgres;Password=postgres"
  }
}
```

### Iniciar o banco

```bash
docker compose up -d
```

### Convenções SQL com PostgreSQL

| Aspecto | SQL Server | PostgreSQL |
|---|---|---|
| Auto-increment | `INT IDENTITY(1,1)` | `SERIAL` ou `GENERATED ALWAYS AS IDENTITY` |
| Strings | `NVARCHAR(n)` | `VARCHAR(n)` |
| Data/hora | `DATETIME2` | `TIMESTAMPTZ` |
| Boolean | `BIT` (0/1) | `BOOLEAN` (true/false) |
| Retornar id inserido | `OUTPUT INSERTED.Id` | `RETURNING id` |
| Paginação | `OFFSET n FETCH NEXT m ROWS ONLY` | `LIMIT m OFFSET n` |
| Nomes de colunas | PascalCase | **snake_case** (convenção PostgreSQL) |
| Data atual | `GETUTCDATE()` | `NOW()` |

O mapeamento automático entre snake_case do banco e PascalCase do C# é feito por `DefaultTypeMap.MatchNamesWithUnderscores = true` na configuração do Dapper.

---

## Migrações — DbUp

As migrações são arquivos SQL numerados sequencialmente, executados automaticamente na inicialização da aplicação pelo **DbUp**.

### Estrutura

```
Infrastructure/
└── Migrations/
    ├── IMigrationRunner.cs
    ├── DbUpMigrationRunner.cs
    └── Scripts/
        ├── 001_CreatePedidosTable.sql
        └── 002_AddPedidosIndex.sql
```

Os scripts são embutidos como `EmbeddedResource` no assembly:

```xml
<!-- TemperatureApi.Infrastructure.csproj -->
<ItemGroup>
  <EmbeddedResource Include="Migrations\Scripts\*.sql" />
</ItemGroup>
```

### Interface e implementação

```csharp
// Permite trocar a implementação nos testes (NoOpMigrationRunner)
public interface IMigrationRunner
{
    void Run();
}

public class DbUpMigrationRunner(string connectionString) : IMigrationRunner
{
    public void Run()
    {
        EnsureDatabase.For.PostgresqlDatabase(connectionString);

        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
            .WithTransactionPerScript()
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();
        if (!result.Successful)
            throw new Exception("Database migration failed.", result.Error);
    }
}
```

O DbUp armazena em `schemaversions` quais scripts já foram executados — re-execuções são seguras.

### Template de script de migração

```sql
-- 001_CreatePedidosTable.sql
CREATE TABLE IF NOT EXISTS pedidos (
    id         SERIAL PRIMARY KEY,
    numero     VARCHAR(50)    NOT NULL,
    total      NUMERIC(18,2)  NOT NULL,
    is_active  BOOLEAN        NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS ix_pedidos_is_active  ON pedidos (is_active);
CREATE INDEX IF NOT EXISTS ix_pedidos_created_at ON pedidos (created_at DESC);
```

Regras para scripts de migração:
- Nome: `NNN_DescricaoEmSnakeCase.sql` — ordena a execução
- Sempre usar `IF NOT EXISTS` — idempotência
- Nunca alterar um script já executado — criar novo script
- Uma migration por feature/tabela

### NuGet necessários (Infrastructure.csproj)

```xml
<PackageReference Include="Npgsql" Version="8.0.5" />
<PackageReference Include="dbup-postgresql" Version="5.0.18" />
<PackageReference Include="Dapper" Version="2.1.28" />
```

---

## Repositório — SQL com PostgreSQL

Diferenças em relação ao padrão SQL Server:

```csharp
// INSERT — retorna id com RETURNING
public async Task<int> CreateAsync(Pedido entity, ...)
{
    const string sql = @"
        INSERT INTO pedidos (numero, total, is_active, created_at)
        VALUES (@Numero, @Total, @IsActive, @CreatedAt)
        RETURNING id";

    return await QueryAsync(conn =>
        conn.ExecuteScalarAsync<int>(sql, entity), cancellationToken);
}

// SELECT paginado — LIMIT / OFFSET
public async Task<(IEnumerable<Pedido>, int)> GetPagedAsync(int page, int pageSize, ...)
{
    const string sql = @"
        SELECT * FROM pedidos WHERE is_active = TRUE
        ORDER BY created_at DESC
        LIMIT @PageSize OFFSET @Offset;

        SELECT COUNT(*) FROM pedidos WHERE is_active = TRUE;";

    return await QueryAsync(async conn =>
    {
        using var multi = await conn.QueryMultipleAsync(sql, new
        {
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        });
        var items = await multi.ReadAsync<Pedido>();
        var total = await multi.ReadFirstAsync<int>();
        return (items, total);
    }, cancellationToken);
}

// DELETE — sempre soft delete
public async Task<bool> DeleteAsync(int id, ...)
{
    const string sql = "UPDATE pedidos SET is_active = FALSE, updated_at = @UpdatedAt WHERE id = @Id";
    var affected = await QueryAsync(conn =>
        conn.ExecuteAsync(sql, new { Id = id, UpdatedAt = DateTime.UtcNow }), cancellationToken);
    return affected > 0;
}
```

---

## Testes Automatizados

### Stack

| Pacote | Versão | Uso |
|---|---|---|
| `xunit` | 2.9+ | Framework de testes |
| `FluentAssertions` | 6.12+ | Asserções legíveis |
| `NSubstitute` | 5.1+ | Mocks/stubs |
| `Microsoft.AspNetCore.Mvc.Testing` | 8.0+ | Host de integração in-process |

### Estrutura de pastas

```
tests/{Nome}.Tests/
├── {Nome}.Tests.csproj
├── Unit/
│   └── Services/
│       └── {Entidade}ServiceTests.cs
└── Integration/
    ├── ApiFactory.cs
    ├── HealthCheckTests.cs
    └── Controllers/
        └── {Entidade}sControllerTests.cs
```

### Testes Unitários — Service

Testam o service em isolamento, substituindo o repositório por um mock do NSubstitute.

```csharp
public class PedidoServiceTests
{
    private readonly IPedidoRepository _repository = Substitute.For<IPedidoRepository>();
    private readonly PedidoService _sut;

    public PedidoServiceTests() => _sut = new PedidoService(_repository);

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsSuccess()
    {
        _repository.GetByIdAsync(1).Returns(new Pedido { Id = 1, Numero = "P001", Total = 100m });

        var result = await _sut.GetByIdAsync(1);

        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsFail()
    {
        _repository.GetByIdAsync(999).ReturnsNull();

        var result = await _sut.GetByIdAsync(999);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("999");
    }

    [Fact]
    public async Task CreateAsync_CallsRepositoryWithMappedEntity()
    {
        var request = new CreatePedidoRequest("P001", 100m);
        _repository.CreateAsync(Arg.Any<Pedido>()).Returns(42);

        await _sut.CreateAsync(request);

        await _repository.Received(1).CreateAsync(
            Arg.Is<Pedido>(p => p.Numero == "P001" && p.Total == 100m));
    }
}
```

### Testes de Integração — Controller

Testam o pipeline HTTP completo (middleware, serialização, status codes) sem banco real. O `ApiFactory` substitui toda a infraestrutura de dados por mocks.

#### ApiFactory

```csharp
public class ApiFactory : WebApplicationFactory<Program>
{
    public IPedidoRepository RepositoryMock { get; } =
        Substitute.For<IPedidoRepository>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Connection string falsa evita exceção no AddInfrastructure
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            { ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test" }));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDbConnectionFactory>();
            services.RemoveAll<IPedidoRepository>();
            services.RemoveAll<IMigrationRunner>();

            services.AddSingleton(RepositoryMock);
            services.AddSingleton<IMigrationRunner, NoOpMigrationRunner>();
        });
    }
}

internal sealed class NoOpMigrationRunner : IMigrationRunner
{
    public void Run() { }
}
```

#### Testes de Health Check

```csharp
public class HealthCheckTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(ApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task GET_Ping_Returns200WithStatusOk()
    {
        var response = await _client.GetAsync("/ping");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("status").GetString().Should().Be("ok");
    }

    [Fact]
    public async Task GET_Health_Returns200WhenHealthy()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("status").GetString().Should().Be("Healthy");
    }
}
```

#### Testes de Controller

```csharp
public class PedidosControllerTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    private readonly IPedidoRepository _repository;

    public PedidosControllerTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
        _repository = factory.RepositoryMock;
    }

    [Fact]
    public async Task GET_ById_WhenExists_Returns200()
    {
        _repository.GetByIdAsync(1).Returns(new Pedido { Id = 1, Numero = "P001", Total = 100m, IsActive = true });

        var response = await _client.GetAsync("/api/v1/pedidos/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_ById_WhenNotFound_Returns404()
    {
        _repository.GetByIdAsync(999).ReturnsNull();

        var response = await _client.GetAsync("/api/v1/pedidos/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_ValidRequest_Returns201WithLocation()
    {
        _repository.CreateAsync(Arg.Any<Pedido>()).Returns(10);

        var request = new CreatePedidoRequest("P001", 100m);
        var response = await _client.PostAsJsonAsync("/api/v1/pedidos", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.ToString().Should().Contain("/10");
    }
}
```

#### Requisito no Program.cs

O `Program` precisa ser acessível para `WebApplicationFactory<Program>`. Adicione ao final do arquivo:

```csharp
public partial class Program { }
```

### Executar os testes

```bash
dotnet test
dotnet test --filter "Unit"        # apenas unitários
dotnet test --filter "Integration" # apenas integração
dotnet test -v normal              # output detalhado
```

---

## Convenções

| Regra | Detalhe |
|---|---|
| Soft delete | Sempre `UPDATE is_active = FALSE`. Nunca `DELETE` físico. |
| `CancellationToken` | Obrigatório em todos os métodos `async`. |
| Nomes de tabela | Plural em snake_case (`pedidos`). Classe no singular PascalCase (`Pedido`). |
| Colunas no banco | Sempre snake_case (`created_at`, `is_active`). Dapper mapeia via `MatchNamesWithUnderscores`. |
| DTOs e Requests | Sempre `record` imutável. |
| Versão da rota | Sempre prefixar com `api/v1/`. |
| Banco padrão | PostgreSQL 16 via Docker para dev local. |
| Migrações | DbUp com scripts `.sql` numerados como `EmbeddedResource`. |
| NuGet (Infrastructure) | `Dapper 2.1.28`, `Npgsql 8.0.5`, `dbup-postgresql 5.0.18`. |
| NuGet (Api) | `Swashbuckle.AspNetCore 6.6.2`. |
| NuGet (Tests) | `xunit 2.9+`, `FluentAssertions 6.12+`, `NSubstitute 5.1+`, `Microsoft.AspNetCore.Mvc.Testing 8.0+`. |
| Target framework | .NET 8 com `Nullable enable` e `ImplicitUsings enable`. |

---

## Checklist para nova entidade

**Domínio e Application**
- [ ] `Domain/Models/{Entidade}.cs` com `Id`, `IsActive`, `CreatedAt`, `UpdatedAt`
- [ ] `Application/DTOs/{Entidade}Dto.cs`
- [ ] `Application/Requests/Create{Entidade}Request.cs` e `Update{Entidade}Request.cs`
- [ ] `Application/Interfaces/I{Entidade}Service.cs`
- [ ] `Application/Services/{Entidade}Service.cs`

**Infrastructure**
- [ ] `Infrastructure/Repositories/Interfaces/I{Entidade}Repository.cs`
- [ ] `Infrastructure/Repositories/{Entidade}Repository.cs` (SQL em snake_case, `RETURNING id`, `LIMIT/OFFSET`)
- [ ] `Infrastructure/Migrations/Scripts/NNN_Create{Entidade}Table.sql` (como `EmbeddedResource`)
- [ ] Registrar no DI de Application e Infrastructure

**Api**
- [ ] `Api/Controllers/{Entidade}sController.cs`
- [ ] `[Produces("application/json")]` e `[ProducesResponseType]` em todos os métodos do controller

**Testes**
- [ ] `tests/.../Unit/Services/{Entidade}ServiceTests.cs` — GetById found/not-found, Create, Update, Delete
- [ ] `tests/.../Integration/Controllers/{Entidade}sControllerTests.cs` — todos os endpoints HTTP
- [ ] `tests/.../Integration/HealthCheckTests.cs` — `/ping` e `/health`

**Infraestrutura local**
- [ ] `docker-compose.yml` com serviço PostgreSQL (use porta diferente de 5432 se houver outro projeto rodando)
- [ ] `AddHealthChecks()` em `Program.cs` + endpoints `/ping` e `/health`
- [ ] Swagger configurado com título e descrição (`SwaggerDoc("v1", ...)`)
- [ ] `public partial class Program { }` no final de `Program.cs`

**Se a API tiver autenticação**
- [ ] `JwtBearer` configurado com `TokenValidationParameters` (HS256, sem validação de issuer/audience para APIs internas)
- [ ] Swagger com `AddSecurityDefinition("Bearer", ...)` + `AddSecurityRequirement`
- [ ] Senhas armazenadas com BCrypt (nunca texto plano)
- [ ] Token com `ClaimTypes.NameIdentifier` para ID do usuário
- [ ] `UseAuthentication()` antes de `UseAuthorization()` no pipeline
