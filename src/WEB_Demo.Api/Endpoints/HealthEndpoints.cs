using Microsoft.Extensions.Diagnostics.HealthChecks;
using WEB_Demo.Api.Models;

namespace WEB_Demo.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/health", async (HealthCheckService healthCheckService) =>
        {
            var report = await healthCheckService.CheckHealthAsync();
            var payload = new
            {
                Status = report.Status.ToString(),
                Service = "WEB_Demo.Api",
                CheckedAt = DateTimeOffset.UtcNow,
                Duration = report.TotalDuration.TotalMilliseconds
            };

            return report.Status == HealthStatus.Healthy
                ? Results.Ok(ApiResponse<object>.Ok(payload))
                : Results.Json(
                    ApiResponse<object>.Fail("Service is unhealthy.", ApiErrorCodes.InternalServerError),
                    statusCode: StatusCodes.Status503ServiceUnavailable);
        });

        return endpoints;
    }
}
