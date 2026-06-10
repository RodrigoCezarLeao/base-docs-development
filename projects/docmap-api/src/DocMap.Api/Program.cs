using System.Reflection;
using System.Text;
using DocMap.Api.Logging;
using DocMap.Api.Middleware;
using DocMap.Application;
using DocMap.Application.Logging;
using DocMap.Infrastructure;
using DocMap.Infrastructure.Migrations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "DocMap API", Version = "v1", Description = "Visual documentation organization API." });
    options.AddSecurityDefinition("Bearer", new() { Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" });
    options.AddSecurityRequirement(new() { { new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } }, [] } });
});

builder.Services.AddHealthChecks();

var jwtSecret = builder.Configuration["Jwt:Secret"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(p =>
        p.WithOrigins(builder.Configuration["Cors:AllowedOrigins"] ?? "http://localhost:5174")
         .AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

// File logging: daily delimited files, captured by a custom provider + request middleware.
builder.Services.AddHttpContextAccessor();
var logsDir = builder.Configuration["Logging:Directory"]
    ?? Environment.GetEnvironmentVariable("LOGS_DIR")
    ?? Path.Combine(builder.Environment.ContentRootPath, "logs");
builder.Services.AddSingleton(new LogReaderOptions { Directory = logsDir });
builder.Logging.AddProvider(new FileLoggerProvider(logsDir, new HttpContextAccessor()));

var app = builder.Build();
app.Services.GetRequiredService<IMigrationRunner>().Run();

if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// App version (manual SemVer from <Version>) + build metadata (set by CI via env).
var appVersion = (Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "0.0.0").Split('+')[0];
var appCommit = Environment.GetEnvironmentVariable("APP_COMMIT") ?? "local";
var appBuiltAt = Environment.GetEnvironmentVariable("APP_BUILD_TIME") ?? "unknown";

app.MapGet("/version", () => Results.Ok(new { name = "DocMap API", version = appVersion, commit = appCommit, builtAt = appBuiltAt })).ExcludeFromDescription();
app.MapGet("/ping", () => Results.Ok(new { status = "ok" })).ExcludeFromDescription();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (ctx, report) =>
    {
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            version = appVersion,
            commit = appCommit,
            checks = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString() })
        }));
    }
});
app.Run();

public partial class Program { }
