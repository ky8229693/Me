using WEB_Demo.Api.Authorization;
using WEB_Demo.Api.Models;

namespace WEB_Demo.Api.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/admin/status", () =>
            Results.Ok(ApiResponse<object>.Ok(new { Status = "Available" }, "Admin access granted.")))
            .RequireAuthorization(AppPolicies.AdminOnly);

        return endpoints;
    }
}
