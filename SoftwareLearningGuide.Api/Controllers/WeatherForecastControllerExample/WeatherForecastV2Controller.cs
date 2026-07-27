using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SoftwareLearningGuide.Api.Controllers.WeatherForecastControllerExample {
    [ApiController]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class WeatherForecastV2Controller(ILogger<WeatherForecastV2Controller> logger) : ControllerBase {
        // Medidor para capturar métricas
        private static readonly Meter MeterInstance = new Meter("SoftwareLearningGuide.Api.Weather", "1.0.0");
        private static readonly Counter<int> ForecastRequests = MeterInstance.CreateCounter<int>(
            "weather.forecast.requests",
            unit: "{requests}",
            description: "Total number of weather forecast requests");

        private static readonly Histogram<double> ForecastGenerationTime = MeterInstance.CreateHistogram<double>(
            "weather.forecast.generation.time",
            unit: "ms",
            description: "Time taken to generate weather forecast data");

        private static readonly string[] Summaries =
        [
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        ];

        [HttpGet(Name = "GetWeatherForecastV2")]
        public IEnumerable<WeatherForecast> Get() {
            // Generar un correlation ID para rastrear toda la solicitud
            var correlationId = HttpContext.TraceIdentifier;
            var requestStartTime = Stopwatch.GetTimestamp();

            // 1. Crear un scope de logging con información contextual relevante
            using (logger.BeginScope(new Dictionary<string, object>
            {
                { "CorrelationId", correlationId },
                { "RequestMethod", HttpContext.Request.Method },
                { "RequestPath", HttpContext.Request.Path },
                { "RemoteIpAddress", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown" },
                { "UserAgent", HttpContext.Request.Headers.UserAgent.ToString() },
                { "Timestamp", DateTime.UtcNow },
                { "ApiVersion", "2.0" }
            })) {
                logger.LogInformation(
                    "Iniciando solicitud de pronóstico del tiempo V2 desde {RemoteIp}",
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

                try {
                    // Registrar métrica: contador de solicitudes
                    ForecastRequests.Add(1, new KeyValuePair<string, object?>("version", "2.0"));

                    var stopwatch = Stopwatch.StartNew();

                    // 2. Generar datos del pronóstico
                    var forecast = Enumerable.Range(1, 5).Select(index => new WeatherForecast {
                        Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                        TemperatureC = Random.Shared.Next(-20, 55),
                        Summary = Summaries[Random.Shared.Next(Summaries.Length)]
                    }).ToArray();

                    stopwatch.Stop();

                    // Registrar métrica: tiempo de generación
                    ForecastGenerationTime.Record(stopwatch.Elapsed.TotalMilliseconds,
                        new KeyValuePair<string, object?>("forecast_count", forecast.Length));

                    logger.LogInformation(
                        "Se generaron {Count} registros meteorológicos exitosamente. " +
                        "Temperatura mínima: {MinTemp}°C, máxima: {MaxTemp}°C. Tiempo procesamiento: {ProcessingTime}ms",
                        forecast.Length,
                        forecast.Min(f => f.TemperatureC),
                        forecast.Max(f => f.TemperatureC),
                        stopwatch.ElapsedMilliseconds);

                    return forecast;
                }
                catch (Exception ex) {
                    logger.LogError(ex, "Error al generar pronóstico del tiempo. CorrelationId: {CorrelationId}",
                        correlationId);
                    throw;
                }
            }
        }
    }
}
