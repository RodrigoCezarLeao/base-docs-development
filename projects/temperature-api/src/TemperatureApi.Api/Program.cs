using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using TemperatureApi.Api.Logging;
using TemperatureApi.Api.Middleware;
using TemperatureApi.Application;
using TemperatureApi.Application.Logging;
using TemperatureApi.Infrastructure;
using TemperatureApi.Infrastructure.Migrations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Temperature API",
        Version = "v1",
        Description = "Temperature monitoring API."
    });
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
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(builder.Configuration["Cors:AllowedOrigins"] ?? "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<ConsentMiddleware>(); // reads X-Tracking-Consent; gates all observers (LGPD)
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseCors();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<MetricsMiddleware>(); // after auth so identity + routed endpoint are available
app.UseMiddleware<AccessTrackingMiddleware>(); // consent-gated user access tracking (LGPD)
app.MapControllers();

// App version (manual SemVer from <Version>) + build metadata (set by CI via env).
var appVersion = (Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "0.0.0").Split('+')[0];
var appCommit = Environment.GetEnvironmentVariable("APP_COMMIT") ?? "local";
var appBuiltAt = Environment.GetEnvironmentVariable("APP_BUILD_TIME") ?? "unknown";

// Version/build info
app.MapGet("/version", () => Results.Ok(new { name = "Temperature API", version = appVersion, commit = appCommit, builtAt = appBuiltAt }))
   .ExcludeFromDescription();

// Liveness — responds immediately, no dependency checks
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
            version = appVersion,
            commit = appCommit,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        });
        await context.Response.WriteAsync(result);
    }
});

app.Run();

public partial class Program { }
