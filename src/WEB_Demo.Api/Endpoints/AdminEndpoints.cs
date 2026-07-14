using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using WEB_Demo.Api.Authorization;
using WEB_Demo.Api.Models;
using WEB_Demo.Api.Services;

namespace WEB_Demo.Api.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/admin/status", () =>
            Results.Ok(ApiResponse<object>.Ok(new { Status = "Available" }, "Admin access granted.")))
            .RequireAuthorization(AppPolicies.AdminOnly);

        var users = endpoints.MapGroup("/api/admin/users")
            .RequireAuthorization(AppPolicies.AdminOnly);

        users.MapGet("/", async (
            int? page,
            int? pageSize,
            string? search,
            AccountAdministrationService service) =>
            ToResult(await service.GetUsersAsync(
                page ?? 1,
                pageSize ?? AccountAdministrationService.DefaultPageSize,
                search)));

        users.MapGet("/{userId}", async (string userId, AccountAdministrationService service) =>
            ToResult(await service.GetUserAsync(userId)));

        users.MapPut("/{userId}/status", async (
            string userId,
            UpdateAccountStatusRequest request,
            ClaimsPrincipal principal,
            UserManager<IdentityUser> userManager,
            AccountAdministrationService service) =>
            ToResult(await service.SetEnabledAsync(
                userManager.GetUserId(principal) ?? string.Empty,
                userId,
                request.IsEnabled)));

        users.MapPut("/{userId}/roles", async (
            string userId,
            UpdateUserRolesRequest request,
            ClaimsPrincipal principal,
            UserManager<IdentityUser> userManager,
            AccountAdministrationService service) =>
            ToResult(await service.SetRolesAsync(
                userManager.GetUserId(principal) ?? string.Empty,
                userId,
                request.Roles)));

        return endpoints;
    }

    private static IResult ToResult<T>(AdministrationResult<T> result) => result.Status switch
    {
        AdministrationResultStatus.Success =>
            Results.Ok(ApiResponse<T>.Ok(result.Data, result.Message)),
        AdministrationResultStatus.ValidationError =>
            Results.BadRequest(ApiResponse<T>.Fail(result.Message, ApiErrorCodes.ValidationError)),
        AdministrationResultStatus.NotFound =>
            Results.NotFound(ApiResponse<T>.Fail(result.Message, ApiErrorCodes.NotFound)),
        AdministrationResultStatus.Conflict =>
            Results.Json(
                ApiResponse<T>.Fail(result.Message, ApiErrorCodes.Conflict),
                statusCode: StatusCodes.Status409Conflict),
        _ => throw new InvalidOperationException("Unknown administration result status.")
    };
}
