using System.Net;
using System.Text.Json;

namespace SoftwareLearningGuide.Api.Middlewares;

/// <summary>
/// Middleware global que captura cualquier excepción no controlada
/// y la loguea antes de devolver una respuesta 500 al cliente.
/// </summary>
public sealed class GlobalExceptionMiddleware {
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger) {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context) {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            { "CorrelationId", context.TraceIdentifier },
            { "RequestMethod", context.Request.Method },
            { "RequestPath", context.Request.Path },
            { "RemoteIpAddress", context.Connection.RemoteIpAddress?.ToString() ?? "unknown" }
        })) {
            try {
                await _next(context);
            }
            catch (Exception ex) {
                var extraProperties = context.Items["LogProperties"] as IDictionary<string, object>;

                var scope = extraProperties is { Count: > 0 }
                    ? _logger.BeginScope(extraProperties)
                    : null;

                try {
                    _logger.LogError(ex,
                        "Excepción no controlada en {Method} {Path}{QueryString}. TraceId: {TraceId} con Mensaje: {Message}",
                        context.Request.Method,
                        context.Request.Path,
                        context.Request.QueryString,
                        context.TraceIdentifier,
                        ex.Message);
                }
                finally {
                    scope?.Dispose();
                }

                await HandleExceptionAsync(context, ex);
            }
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception) {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new {
            error = "An unexpected error occurred.",
            traceId = context.TraceIdentifier
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
