using TemperatureApi.Api.Middleware;
using TemperatureApi.Application;
using TemperatureApi.Infrastructure;
using TemperatureApi.Infrastructure.Migrations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Temperature API", Version = "v1" });
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(builder.Configuration["Cors:AllowedOrigins"] ?? "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Executa as migrations antes de aceitar requisições
app.Services.GetRequiredService<IMigrationRunner>().Run();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Expõe Program para WebApplicationFactory nos testes
public partial class Program { }
