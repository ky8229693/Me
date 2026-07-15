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
            .WithTags("Administration")
            .RequireAuthorization(AppPolicies.AdminOnly);

        var users = endpoints.MapGroup("/api/admin/users")
            .WithTags("Administration - Users")
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

        var roles = endpoints.MapGroup("/api/admin/roles")
            .WithTags("Administration - Roles")
            .RequireAuthorization(AppPolicies.AdminOnly);

        roles.MapGet("/", async (RoleAdministrationService service) =>
            ToResult(await service.GetRolesAsync()));

        roles.MapPost("/", async (CreateRoleRequest request, RoleAdministrationService service) =>
        {
            var result = await service.CreateAsync(request.Name);
            return result.Status == AdministrationResultStatus.Success
                ? Results.Created($"/api/admin/roles/{result.Data!.Id}", ApiResponse<AdminRole>.Ok(result.Data, result.Message))
                : ToResult(result);
        });

        roles.MapPut("/{roleId}", async (
            string roleId,
            UpdateRoleRequest request,
            RoleAdministrationService service) =>
            ToResult(await service.RenameAsync(roleId, request.Name)));

        roles.MapDelete("/{roleId}", async (string roleId, RoleAdministrationService service) =>
            ToResult(await service.DeleteAsync(roleId)));

        return endpoints;
    }

    internal static IResult ToResult<T>(AdministrationResult<T> result) => result.Status switch
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
