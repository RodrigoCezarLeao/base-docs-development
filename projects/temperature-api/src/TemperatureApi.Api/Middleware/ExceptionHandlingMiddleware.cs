using System.Net;
using System.Text.Json;
using TemperatureApi.Application.Responses;

namespace TemperatureApi.Api.Middleware;

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
            ArgumentException           => (int)HttpStatusCode.BadRequest,
            KeyNotFoundException        => (int)HttpStatusCode.NotFound,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            _                           => (int)HttpStatusCode.InternalServerError
        };

        var isServerError = context.Response.StatusCode == (int)HttpStatusCode.InternalServerError;
        var response = ApiResponse<object>.Fail(
            isServerError ? "An internal server error occurred." : exception.Message);

        await context.Response.WriteAsync(JsonSerializer.Serialize(response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
