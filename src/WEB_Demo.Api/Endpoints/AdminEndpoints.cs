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
            .WithTags("Administration - Users");

        users.MapGet("/", async (
            int? page,
            int? pageSize,
            string? search,
            AccountAdministrationService service) =>
            ToResult(await service.GetUsersAsync(
                page ?? 1,
                pageSize ?? AccountAdministrationService.DefaultPageSize,
                search))).RequireAuthorization(PermissionPolicyProvider.For(AppPermissions.UsersRead));

        users.MapGet("/{userId}", async (string userId, AccountAdministrationService service) =>
            ToResult(await service.GetUserAsync(userId)))
            .RequireAuthorization(PermissionPolicyProvider.For(AppPermissions.UsersRead));

        users.MapPut("/{userId}/status", async (
            string userId,
            UpdateAccountStatusRequest request,
            ClaimsPrincipal principal,
            UserManager<IdentityUser> userManager,
            AccountAdministrationService service) =>
            ToResult(await service.SetEnabledAsync(
                userManager.GetUserId(principal) ?? string.Empty,
                userId,
                request.IsEnabled))).RequireAuthorization(PermissionPolicyProvider.For(AppPermissions.UsersManage));

        users.MapPut("/{userId}/roles", async (
            string userId,
            UpdateUserRolesRequest request,
            ClaimsPrincipal principal,
            UserManager<IdentityUser> userManager,
            AccountAdministrationService service) =>
            ToResult(await service.SetRolesAsync(
                userManager.GetUserId(principal) ?? string.Empty,
                userId,
                request.Roles))).RequireAuthorization(PermissionPolicyProvider.For(AppPermissions.UsersManage));

        var roles = endpoints.MapGroup("/api/admin/roles")
            .WithTags("Administration - Roles");

        roles.MapGet("/", async (RoleAdministrationService service) =>
            ToResult(await service.GetRolesAsync()))
            .RequireAuthorization(PermissionPolicyProvider.For(AppPermissions.RolesRead));

        roles.MapPost("/", async (CreateRoleRequest request, RoleAdministrationService service) =>
        {
            var result = await service.CreateAsync(request.Name);
            return result.Status == AdministrationResultStatus.Success
                ? Results.Created($"/api/admin/roles/{result.Data!.Id}", ApiResponse<AdminRole>.Ok(result.Data, result.Message))
                : ToResult(result);
        }).RequireAuthorization(PermissionPolicyProvider.For(AppPermissions.RolesManage));

        roles.MapPut("/{roleId}", async (
            string roleId,
            UpdateRoleRequest request,
            RoleAdministrationService service) =>
            ToResult(await service.RenameAsync(roleId, request.Name)))
            .RequireAuthorization(PermissionPolicyProvider.For(AppPermissions.RolesManage));

        roles.MapDelete("/{roleId}", async (string roleId, RoleAdministrationService service) =>
            ToResult(await service.DeleteAsync(roleId)))
            .RequireAuthorization(PermissionPolicyProvider.For(AppPermissions.RolesManage));

        endpoints.MapGet("/api/admin/permissions", (PermissionAdministrationService service) =>
            Results.Ok(ApiResponse<IReadOnlyCollection<PermissionInfo>>.Ok(service.GetCatalog())))
            .WithTags("Administration - Permissions")
            .RequireAuthorization(PermissionPolicyProvider.For(AppPermissions.PermissionsManage));

        roles.MapGet("/{roleId}/permissions", async (string roleId, PermissionAdministrationService service) =>
            ToResult(await service.GetRoleAsync(roleId)))
            .RequireAuthorization(PermissionPolicyProvider.For(AppPermissions.PermissionsManage));

        roles.MapPut("/{roleId}/permissions", async (string roleId, UpdateRolePermissionsRequest request, PermissionAdministrationService service) =>
            ToResult(await service.SetRoleAsync(roleId, request.Permissions)))
            .RequireAuthorization(PermissionPolicyProvider.For(AppPermissions.PermissionsManage));

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
        AdministrationResultStatus.Forbidden =>
            Results.Json(ApiResponse<T>.Fail(result.Message, ApiErrorCodes.Forbidden), statusCode: StatusCodes.Status403Forbidden),
        AdministrationResultStatus.Conflict =>
            Results.Json(
                ApiResponse<T>.Fail(result.Message, ApiErrorCodes.Conflict),
                statusCode: StatusCodes.Status409Conflict),
        _ => throw new InvalidOperationException("Unknown administration result status.")
    };
}
