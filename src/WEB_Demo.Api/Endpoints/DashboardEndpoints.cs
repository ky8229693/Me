using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using WEB_Demo.Api.Models;
using WEB_Demo.Api.Services;

namespace WEB_Demo.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/dashboard", async (ClaimsPrincipal principal,
            UserManager<IdentityUser> users, DashboardService service) =>
            Results.Ok(ApiResponse<DashboardInfo>.Ok(
                await service.GetAsync(users.GetUserId(principal) ?? string.Empty))))
            .WithTags("Dashboard").WithName("GetDashboard").RequireAuthorization();
        return endpoints;
    }
}
