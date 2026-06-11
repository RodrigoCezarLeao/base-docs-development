# Guideline — REST API in C# with Controller / Service / Repository + Dapper

## Purpose

This document defines the adopted standard for building RESTful APIs in C# .NET 8. The goal is to ensure consistency across projects: the same folder structure, the same naming conventions, the same response envelope, and the same HTTP behavior.

Any developer (or Claude) should be able to create a new entity by following this guide without having to invent structure.

---

## Project Structure

Every API follows a solution with **4 projects separated by responsibility**:

```
{SolutionName}.sln
src/
├── {Name}.Api             → HTTP entry point: controllers, middleware, Program.cs
├── {Name}.Application     → Business rules: services, DTOs, requests, responses
├── {Name}.Domain          → Domain entities: pure models
└── {Name}.Infrastructure  → Data access: Dapper, repositories, connection factory, migrations
tests/
└── {Name}.Tests           → Unit tests (Unit/) and integration tests (Integration/)
docker-compose.yml         → PostgreSQL database for local development
```

**Dependency direction** (never reversed):

```
Api → Application → Domain
         ↑
  Infrastructure → Domain
```

Infrastructure knows Application (uses its interfaces), but Application never references Infrastructure directly — only via contract.

---

## Domain Layer

Contains only **entities that mirror database tables**. No business logic, no framework attributes, no external dependencies.

Every model must include the required control fields:

```csharp
namespace {Name}.Domain.Models;

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

## Application Layer

### DTOs

The API **output** object. Always an immutable `record`. Never expose sensitive fields (passwords, tokens, internal data).

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

The API **input** object. Always a `record` with `DataAnnotations` for validation.

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

### Response Envelope

**Every** API response is wrapped in `ApiResponse<T>`. Never return the object directly.

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

Success format:
```json
{ "success": true, "data": { ... }, "message": null, "errors": null }
```

Error format:
```json
{ "success": false, "data": null, "message": "Pedido with id 5 not found.", "errors": null }
```

### Pagination

Always paginate listings. Use `PagedResponse<T>`:

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

The service orchestrates business logic. It:
- Calls the repository via interface
- Validates business rules (not format validation — that's the request's responsibility)
- Maps Model → DTO
- Returns `ApiResponse<T>`

```csharp
public class PedidoService(IPedidoRepository repository) : IPedidoService
{
    public async Task<ApiResponse<PedidoDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var pedido = await repository.GetByIdAsync(id, cancellationToken);
        if (pedido is null)
            return ApiResponse<PedidoDto>.Fail($"Pedido with id {id} not found.");

        return ApiResponse<PedidoDto>.Ok(MapToDto(pedido));
    }

    // ... other methods

    private static PedidoDto MapToDto(Pedido p) =>
        new(p.Id, p.Numero, p.Total, p.IsActive, p.CreatedAt, p.UpdatedAt);
}
```

The service **does not** throw an exception when a record is not found — it returns `Fail()`. Exceptions are for unexpected errors, not business flow.

---

## Infrastructure Layer

### Connection Factory

Centralizes database connection creation. Registered as `Singleton` in DI.

```csharp
public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

// PostgreSQL (default)
public class DbConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public IDbConnection CreateConnection() => new NpgsqlConnection(connectionString);
}
```

### BaseRepository

Every repository inherits from `BaseRepository<T, TKey>`, which provides two helpers:

```csharp
public abstract class BaseRepository<T, TKey>(IDbConnectionFactory connectionFactory)
    : IBaseRepository<T, TKey> where T : class
{
    // Opens and closes the connection automatically
    protected async Task<TResult> QueryAsync<TResult>(
        Func<IDbConnection, Task<TResult>> query,
        CancellationToken cancellationToken = default)
    {
        using var connection = ConnectionFactory.CreateConnection();
        return await query(connection);
    }

    // Opens connection, executes in transaction, automatic commit or rollback
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

`IBaseRepository<T, TKey>` defines the CRUD contract:

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

### Concrete Repository

Each entity has its own interface (for specific queries) and implementation:

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
        // QueryMultiple executes both queries in a single database round trip
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
        // Always soft delete — never physical DELETE
        const string sql = "UPDATE Pedidos SET IsActive = 0, UpdatedAt = @UpdatedAt WHERE Id = @Id";
        var affected = await QueryAsync(conn =>
            conn.ExecuteAsync(sql, new { Id = id, UpdatedAt = DateTime.UtcNow }), cancellationToken);
        return affected > 0;
    }
}
```

---

## Api Layer

### Controller

Single responsibility: receive the HTTP request, call the service, and return the correct status code.

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

| Operation | Route | Success | Not found | Invalid |
|---|---|---|---|---|
| List | `GET /api/v1/pedidos` | `200` | — | — |
| Get by ID | `GET /api/v1/pedidos/{id}` | `200` | `404` | — |
| Create | `POST /api/v1/pedidos` | `201` | — | `400` |
| Update | `PUT /api/v1/pedidos/{id}` | `200` | `404` | `400` |
| Delete | `DELETE /api/v1/pedidos/{id}` | `200` | `404` | — |

The `400` from request validation is automatic via `[ApiController]` + `DataAnnotations`.

### Exception Middleware

Catches unhandled exceptions and returns a standardized `ApiResponse<T>`:

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
        Title = "My API",
        Version = "v1",
        Description = "API description."
    });
});

builder.Services.AddHealthChecks();
// To verify database connectivity, add:
// builder.Services.AddHealthChecks().AddNpgsql(connectionString);
// (requires NuGet: AspNetCore.HealthChecks.Npgsql)

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
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthorization();
app.MapControllers();

// Liveness — no dependencies, always responds
app.MapGet("/ping", () => Results.Ok(new { status = "ok" }))
   .ExcludeFromDescription();

// Readiness — checks health of registered dependencies
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

// Exposes Program for WebApplicationFactory in integration tests
public partial class Program { }
```

### launchSettings.json

Without this file, `dotnet run` defaults to the `Production` environment — Swagger is hidden and `UseHttpsRedirection` triggers a warning because no HTTPS port is configured locally.

Create `src/{Name}.Api/Properties/launchSettings.json`:

```json
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:{port}",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

With this in place, `dotnet run` automatically sets `Development` and binds to the correct port with no extra flags.

`UseHttpsRedirection` must be guarded so it only runs outside Development (where HTTPS is actually configured):

```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
```

### Swagger

Swagger is configured via `Swashbuckle.AspNetCore` and exposed only in `Development`.

```csharp
// Registration
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title  = "My API",
        Version = "v1",
        Description = "API description."
    });
});

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // UI at /swagger
}
```

For endpoints to appear correctly in Swagger, controllers must declare `[Produces("application/json")]` and methods must use `[ProducesResponseType]`:

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

UI available at: `http://localhost:{port}/swagger`

### Health Checks

Every API must expose two health check endpoints:

| Endpoint | Purpose | Checks database? |
|---|---|---|
| `GET /ping` | Liveness — is the application alive? | No |
| `GET /health` | Readiness — are dependencies available? | Yes (if configured) |

`/ping` is a simple minimal API endpoint with no dependency overhead. It is used by orchestrators (Kubernetes, Railway, Render) to know if the process is running.

`/health` uses the ASP.NET Core health checks system. In production, add database verification:

```bash
dotnet add package AspNetCore.HealthChecks.Npgsql
```

```csharp
builder.Services.AddHealthChecks()
    .AddNpgsql(connectionString, name: "postgresql");
```

`/health` response (custom JSON):
```json
{
  "status": "Healthy",
  "checks": [
    { "name": "postgresql", "status": "Healthy", "description": null }
  ]
}
```

When a dependency fails, `/health` returns `503 Service Unavailable` with `"status": "Unhealthy"`.

---

## JWT Authentication (optional)

When the API needs authentication, use **JWT Bearer with HS256**. Add to `Api.csproj`:

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.8" />
```

`appsettings.json`:
```json
{
  "Jwt": { "Secret": "your-secret-min-32-chars-replace-in-production!" }
}
```

`Program.cs` — register the authentication middleware and Swagger with Bearer support:

```csharp
// Registration
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

// Swagger with Bearer
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

// Middleware (order matters — Authentication before Authorization)
app.UseAuthentication();
app.UseAuthorization();
```

**Token generation** in `AuthService`:

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

**In controllers** — protect with `[Authorize]` and read the user ID via `ClaimTypes.NameIdentifier`:

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

**Role-based authorization (admin)** — add an `is_admin BOOLEAN NOT NULL DEFAULT FALSE`
column to `users`, emit a `Role` claim when the user is an admin, and protect admin-only
endpoints with `[Authorize(Roles = "Admin")]`:

```csharp
if (user.IsAdmin)
    claims.Add(new Claim(ClaimTypes.Role, "Admin"));
```

Designate the first admin manually: `UPDATE users SET is_admin = TRUE WHERE email = '...';`

**Passwords** — never store plain text. Use BCrypt:

```xml
<PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
```

```csharp
// Hash when creating a user
string hash = BCrypt.Net.BCrypt.HashPassword(plainPassword);

// Verification on login
bool isValid = BCrypt.Net.BCrypt.Verify(plainPassword, storedHash);
```

---

## File logging & admin log viewer (optional)

For VPS deployments the API writes **structured daily log files** that an admin can
browse from the frontend. See `temperature-api`/`docmap-api` for the reference.

**Format** — one delimited line per entry in `logs/yyyy-MM-dd.txt` (a new file per day;
the date prefix sorts newest on top). The message is last so embedded separators are safe:

```
2026-06-10T13:00:00.123Z | INFO | Api.HTTP | <requestId> | <userId> | GET /api/v1/x -> 200 (12ms)
```

**Writing** (`Api/Logging/`):
- `FileLoggerProvider` + `FileLogger` (an `ILoggerProvider`) capture every `ILogger`
  entry; `requestId` = `HttpContext.TraceIdentifier`, `userId` = the `NameIdentifier`
  claim (via `IHttpContextAccessor`).
- `FileLogWriter` appends under a lock with `FileShare.ReadWrite`, wrapped in `try/catch`
  so **logging never throws into the request pipeline**.
- `RequestLoggingMiddleware` logs one line per HTTP request.
- Wire in `Program.cs`: `AddHttpContextAccessor()`, resolve the dir from
  `Logging:Directory` / `LOGS_DIR` env (default `<contentRoot>/logs`), then
  `builder.Logging.AddProvider(new FileLoggerProvider(dir, new HttpContextAccessor()))`.

**Reading** (`Application/Logging/`): `LogReaderService` parses, filters (date, level,
category, userId, free-text) and paginates a day's file, newest-first. The `date` is
**guarded to `^\d{4}-\d{2}-\d{2}$`** (no path traversal).

**Endpoints** — `LogsController` with `[Authorize(Roles = "Admin")]`:
`GET /api/v1/admin/logs/files` (available days) and `GET /api/v1/admin/logs?date&level&category&userId&q&page&pageSize`.

> On the VPS, mount the logs dir as a volume so files persist and the viewer can read
> them — see `guidelines/infra-devops.md`.

---

## Caching (IMemoryCache) — for rarely-changing reads

For values that change rarely (reference/lookup data, historical records), cache them
in-process so they aren't re-read from PostgreSQL on every request. Use **cache-aside**
with a TTL backstop and **invalidate on writes**.

- `Application/Caching/ICacheService.cs` — `GetOrCreateAsync<T>(key, factory, ttl?)`,
  `Remove(key)`, `RemoveByPrefix(prefix)`. Keys live in a `CacheKeys` static.
- `Infrastructure/Caching/MemoryCacheService.cs` — implements it over `IMemoryCache`
  (default TTL ~10 min; tracks live keys so `RemoveByPrefix` works).
- DI (`Infrastructure/DependencyInjection.cs`): `services.AddMemoryCache();` +
  `services.AddSingleton<ICacheService, MemoryCacheService>();` (csproj:
  `Microsoft.Extensions.Caching.Memory`).

In the **service** (not the repository — keep repos pure SQL):

```csharp
public async Task<ApiResponse<Dto>> GetByIdAsync(int id, CancellationToken ct = default)
{
    var entity = await cache.GetOrCreateAsync(CacheKeys.Thing(id),
        () => repository.GetByIdAsync(id, ct), cancellationToken: ct);
    // ...
}

// On every write, drop the affected key:
public async Task<...> UpdateAsync(int id, ...) { /* ... */ cache.Remove(CacheKeys.Thing(id)); }
```

> Fits the single-replica-per-project infra. For multi-instance you'd swap the
> implementation for a distributed cache (e.g. Redis) behind the same `ICacheService`.
> Don't cache fast-changing data; cache the stable part and read volatile bits fresh
> (e.g. cache the project entity, read its document count live).

---

## Metrics (in-process, real-time)

Lightweight monitoring without Prometheus/Grafana: a singleton `IMetricsCollector`
(`Infrastructure/Metrics/MetricsCollector.cs`) is fed by `MetricsMiddleware` on every request
and read by an admin endpoint.

- The middleware records: total, in-flight (try/finally), the active identity
  (`NameIdentifier` claim ?? IP), a per-second traffic bucket (60 s ring), and the per-endpoint
  count keyed by the **route template** (low cardinality). It skips `/api/v1/admin/metrics`,
  `/health`, `/ping`, `/version` so the dashboard's own polling isn't measured.
- `GET /api/v1/admin/metrics` (`[Authorize(Roles="Admin")]`) returns a `MetricsSnapshot`
  (active users, in-flight, total, endpoints, 60 s traffic).
- DI: `services.AddSingleton<IMetricsCollector, MetricsCollector>();`; place
  `app.UseMiddleware<MetricsMiddleware>()` after auth, before `MapControllers`.

> In-memory, per-process (resets on restart; fits the single-replica infra). If you later need
> history, alerting or multi-instance aggregation, graduate to Prometheus + Grafana.

---

## Dependency Injection

Each layer exposes a registration extension method. `Program.cs` calls only these two methods.

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

    // Automatically maps PostgreSQL snake_case to C# PascalCase
    DefaultTypeMap.MatchNamesWithUnderscores = true;

    services.AddSingleton<IDbConnectionFactory>(_ => new DbConnectionFactory(cs));
    services.AddSingleton<IMigrationRunner>(_ => new DbUpMigrationRunner(cs));
    services.AddScoped<IPedidoRepository, PedidoRepository>();
    return services;
}
```

---

## Database — PostgreSQL

The default database for local development is **PostgreSQL 16** running via Docker.

### docker-compose.yml

```yaml
services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: my_db
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres -d my_db"]
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
    "DefaultConnection": "Host=localhost;Port=5432;Database=my_db;Username=postgres;Password=postgres"
  }
}
```

### Starting the database

```bash
docker compose up -d
```

### SQL conventions with PostgreSQL

| Aspect | SQL Server | PostgreSQL |
|---|---|---|
| Auto-increment | `INT IDENTITY(1,1)` | `SERIAL` or `GENERATED ALWAYS AS IDENTITY` |
| Strings | `NVARCHAR(n)` | `VARCHAR(n)` |
| Date/time | `DATETIME2` | `TIMESTAMPTZ` |
| Boolean | `BIT` (0/1) | `BOOLEAN` (true/false) |
| Return inserted id | `OUTPUT INSERTED.Id` | `RETURNING id` |
| Pagination | `OFFSET n FETCH NEXT m ROWS ONLY` | `LIMIT m OFFSET n` |
| Column names | PascalCase | **snake_case** (PostgreSQL convention) |
| Current date | `GETUTCDATE()` | `NOW()` |

The automatic mapping between database snake_case and C# PascalCase is done by `DefaultTypeMap.MatchNamesWithUnderscores = true` in the Dapper configuration.

---

## Migrations — DbUp

Migrations are sequentially numbered SQL files, executed automatically at application startup by **DbUp**.

### Structure

```
Infrastructure/
└── Migrations/
    ├── IMigrationRunner.cs
    ├── DbUpMigrationRunner.cs
    └── Scripts/
        ├── 001_CreatePedidosTable.sql
        └── 002_AddPedidosIndex.sql
```

Scripts are embedded as `EmbeddedResource` in the assembly:

```xml
<!-- TemperatureApi.Infrastructure.csproj -->
<ItemGroup>
  <EmbeddedResource Include="Migrations\Scripts\*.sql" />
</ItemGroup>
```

### Interface and implementation

```csharp
// Allows swapping the implementation in tests (NoOpMigrationRunner)
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

DbUp stores in `schemaversions` which scripts have already been executed — re-runs are safe.

### Migration script template

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

Migration script rules:
- Name: `NNN_DescriptionInSnakeCase.sql` — determines execution order
- Always use `IF NOT EXISTS` — idempotency
- Never alter an already-executed script — create a new one
- One migration per feature/table

### Required NuGet packages (Infrastructure.csproj)

```xml
<PackageReference Include="Npgsql" Version="8.0.5" />
<PackageReference Include="dbup-postgresql" Version="5.0.18" />
<PackageReference Include="Dapper" Version="2.1.28" />
```

---

## Repository — SQL with PostgreSQL

Differences from the SQL Server pattern:

```csharp
// INSERT — returns id with RETURNING
public async Task<int> CreateAsync(Pedido entity, ...)
{
    const string sql = @"
        INSERT INTO pedidos (numero, total, is_active, created_at)
        VALUES (@Numero, @Total, @IsActive, @CreatedAt)
        RETURNING id";

    return await QueryAsync(conn =>
        conn.ExecuteScalarAsync<int>(sql, entity), cancellationToken);
}

// Paginated SELECT — LIMIT / OFFSET
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

// DELETE — always soft delete
public async Task<bool> DeleteAsync(int id, ...)
{
    const string sql = "UPDATE pedidos SET is_active = FALSE, updated_at = @UpdatedAt WHERE id = @Id";
    var affected = await QueryAsync(conn =>
        conn.ExecuteAsync(sql, new { Id = id, UpdatedAt = DateTime.UtcNow }), cancellationToken);
    return affected > 0;
}
```

---

## Automated Tests

### Stack

| Package | Version | Use |
|---|---|---|
| `xunit` | 2.9+ | Test framework |
| `FluentAssertions` | 6.12+ | Readable assertions |
| `NSubstitute` | 5.1+ | Mocks/stubs |
| `Microsoft.AspNetCore.Mvc.Testing` | 8.0+ | In-process integration host |

### Folder structure

```
tests/{Name}.Tests/
├── {Name}.Tests.csproj
├── Unit/
│   └── Services/
│       └── {Entity}ServiceTests.cs
└── Integration/
    ├── ApiFactory.cs
    ├── HealthCheckTests.cs
    └── Controllers/
        └── {Entity}sControllerTests.cs
```

### Unit Tests — Service

Test the service in isolation, replacing the repository with an NSubstitute mock.

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

### Integration Tests — Controller

Test the full HTTP pipeline (middleware, serialization, status codes) without a real database. The `ApiFactory` replaces all data infrastructure with mocks.

#### ApiFactory

```csharp
public class ApiFactory : WebApplicationFactory<Program>
{
    public IPedidoRepository RepositoryMock { get; } =
        Substitute.For<IPedidoRepository>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Fake connection string avoids exception in AddInfrastructure
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

#### Health Check Tests

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

#### Controller Tests

> **Note — `CancellationToken` in integration mocks**
>
> ASP.NET Core injects a real `CancellationToken` (tied to the HTTP request lifecycle) into controller parameters. NSubstitute does **exact** matching on all arguments; if the mock was configured with `GetByIdAsync(1)` (which compiles as `GetByIdAsync(1, CancellationToken.None)`), it will **not** match `GetByIdAsync(1, httpContextToken)` and will silently return `null`.
>
> Always use `Arg.Any<CancellationToken>()` in all integration setups. This does not affect unit tests (where the passed token is `default`).

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
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new Pedido { Id = 1, Numero = "P001", Total = 100m, IsActive = true });

        var response = await _client.GetAsync("/api/v1/pedidos/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_ById_WhenNotFound_Returns404()
    {
        _repository.GetByIdAsync(999, Arg.Any<CancellationToken>()).ReturnsNull();

        var response = await _client.GetAsync("/api/v1/pedidos/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_ValidRequest_Returns201WithLocation()
    {
        _repository.CreateAsync(Arg.Any<Pedido>(), Arg.Any<CancellationToken>()).Returns(10);

        var request = new CreatePedidoRequest("P001", 100m);
        var response = await _client.PostAsJsonAsync("/api/v1/pedidos", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.ToString().Should().Contain("/10");
    }
}
```

#### Program.cs requirement

The `Program` class must be accessible to `WebApplicationFactory<Program>`. Add to the end of the file:

```csharp
public partial class Program { }
```

### Running tests

```bash
dotnet test
dotnet test --filter "Unit"        # unit tests only
dotnet test --filter "Integration" # integration tests only
dotnet test -v normal              # verbose output
```

---

## Conventions

| Rule | Detail |
|---|---|
| Soft delete | Always `UPDATE is_active = FALSE`. Never physical `DELETE`. |
| `CancellationToken` | Required in all `async` methods. |
| Table names | Plural snake_case (`pedidos`). Singular PascalCase class name (`Pedido`). |
| Database columns | Always snake_case (`created_at`, `is_active`). Dapper maps via `MatchNamesWithUnderscores`. |
| DTOs and Requests | Always immutable `record`. |
| Route version | Always prefix with `api/v1/`. |
| Default database | PostgreSQL 16 via Docker for local development. |
| Migrations | DbUp with numbered `.sql` scripts as `EmbeddedResource`. |
| NuGet (Infrastructure) | `Dapper 2.1.28`, `Npgsql 8.0.5`, `dbup-postgresql 5.0.18`. |
| NuGet (Api) | `Swashbuckle.AspNetCore 6.6.2`. |
| NuGet (Tests) | `xunit 2.9+`, `FluentAssertions 6.12+`, `NSubstitute 5.1+`, `Microsoft.AspNetCore.Mvc.Testing 8.0+`. |
| Target framework | .NET 8 with `Nullable enable` and `ImplicitUsings enable`. |

---

## Security — NuGet

Every backend project must include two files at the solution root to isolate package resolution and pin transitive dependencies.

### `NuGet.Config`

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <!--
      <clear /> removes ALL inherited sources — machine-level and user-level NuGet.Config
      files are ignored. Corporate feeds on the developer's machine will not bleed in.
    -->
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>

  <!--
    Package Source Mapping (NuGet 6.0+) — maps every package pattern to nuget.org.
    Second layer of defense against dependency confusion attacks.
  -->
  <packageSourceMapping>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
```

### `Directory.Build.props`

```xml
<Project>
  <PropertyGroup>
    <!--
      Generates packages.lock.json - pins the exact resolved version of every
      transitive dependency. Equivalent to pnpm-lock.yaml. Commit this file.

      In CI, pass -p:RestoreLockedMode=true to dotnet restore/build to fail
      if the lockfile is out of date (equivalent to pnpm frozen-lockfile).

      NOTE: XML comments cannot contain double-dash sequences.
      Write CLI flags as single words, not with their leading dashes.
    -->
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
  </PropertyGroup>
</Project>
```

### What each file does

| File | Effect |
|---|---|
| `NuGet.Config` with `<clear />` | Removes all inherited package sources — corporate or local feeds on the machine are ignored |
| Package Source Mapping | Every package ID pattern is explicitly mapped to nuget.org; prevents ambiguous resolution |
| `Directory.Build.props` | Generates `packages.lock.json` with the exact resolved version of every direct and transitive dependency |
| `RestoreLockedMode=true` (CI flag) | Makes the build fail if the lockfile is stale — equivalent to `--frozen-lockfile` |

### Compared to the frontend approach

| Concern | Frontend (`.npmrc`) | Backend (`NuGet.Config`) |
|---|---|---|
| Registry isolation | `registry=https://registry.npmjs.org/` | `<clear />` + explicit source |
| Prevent ambiguous resolution | — | Package Source Mapping |
| Lockfile | `pnpm-lock.yaml` (always) | `packages.lock.json` (opt-in via `Directory.Build.props`) |
| Strict CI restore | `pnpm install --frozen-lockfile` | `-p:RestoreLockedMode=true` |
| Block install scripts | `ignore-scripts=true` | N/A — NuGet doesn't run install scripts |
| Exact versions | `save-exact=true` | `.csproj` already uses exact versions by default |

> **`dotnet nuget audit`** — run `dotnet nuget audit` to check all packages against the GitHub Advisory Database for known CVEs. Add it as a CI step alongside `dotnet build`.

---

## Checklist for a new entity

**Domain and Application**
- [ ] `Domain/Models/{Entity}.cs` with `Id`, `IsActive`, `CreatedAt`, `UpdatedAt`
- [ ] `Application/DTOs/{Entity}Dto.cs`
- [ ] `Application/Requests/Create{Entity}Request.cs` and `Update{Entity}Request.cs`
- [ ] `Application/Interfaces/I{Entity}Service.cs`
- [ ] `Application/Services/{Entity}Service.cs`

**Infrastructure**
- [ ] `Application/Interfaces/I{Entity}Repository.cs` (contract lives in Application, not Infrastructure)
- [ ] `Infrastructure/Repositories/{Entity}Repository.cs` (SQL in snake_case, `RETURNING id`, `LIMIT/OFFSET`)
- [ ] `Infrastructure/Migrations/Scripts/NNN_Create{Entity}Table.sql` (as `EmbeddedResource`)
- [ ] Register in Application and Infrastructure DI

**Api**
- [ ] `Api/Controllers/{Entity}sController.cs`
- [ ] `[Produces("application/json")]` and `[ProducesResponseType]` on all controller methods

**Tests**
- [ ] `tests/.../Unit/Services/{Entity}ServiceTests.cs` — GetById found/not-found, Create, Update, Delete
- [ ] `tests/.../Integration/Controllers/{Entity}sControllerTests.cs` — all HTTP endpoints
- [ ] `tests/.../Integration/HealthCheckTests.cs` — `/ping` and `/health`

**Local infrastructure**
- [ ] `NuGet.Config` at solution root with `<clear />`, nuget.org source, and package source mapping
- [ ] `Directory.Build.props` at solution root with `RestorePackagesWithLockFile=true`
- [ ] `docker-compose.yml` with PostgreSQL service (use a port other than 5432 if another project is running)
- [ ] `Properties/launchSettings.json` with `ASPNETCORE_ENVIRONMENT=Development` and the correct port
- [ ] `UseHttpsRedirection` guarded with `if (!app.Environment.IsDevelopment())`
- [ ] `AddHealthChecks()` in `Program.cs` + `/ping` and `/health` endpoints
- [ ] Swagger configured with title and description (`SwaggerDoc("v1", ...)`)
- [ ] `public partial class Program { }` at the end of `Program.cs`

**If the API has authentication**
- [ ] `JwtBearer` configured with `TokenValidationParameters` (HS256, no issuer/audience validation for internal APIs)
- [ ] Swagger with `AddSecurityDefinition("Bearer", ...)` + `AddSecurityRequirement`
- [ ] Passwords stored with BCrypt (never plain text)
- [ ] Token with `ClaimTypes.NameIdentifier` for user ID
- [ ] `UseAuthentication()` before `UseAuthorization()` in the pipeline
