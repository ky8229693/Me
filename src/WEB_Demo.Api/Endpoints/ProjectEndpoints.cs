using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using WEB_Demo.Api.Authorization;
using WEB_Demo.Api.Models;
using WEB_Demo.Api.Services;

namespace WEB_Demo.Api.Endpoints;

public static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var projects = endpoints.MapGroup("/api/projects")
            .WithTags("Projects")
            .RequireAuthorization();

        projects.MapPost("/", async (CreateProjectRequest request, ClaimsPrincipal principal,
            UserManager<IdentityUser> users, ProjectAdministrationService service) =>
        {
            var result = await service.CreateAsync(users.GetUserId(principal) ?? string.Empty, request.Name, request.Description);
            return result.Status == AdministrationResultStatus.Success
                ? Results.Created($"/api/projects/{result.Data!.Id}", ApiResponse<ProjectInfo>.Ok(result.Data, result.Message))
                : ToResult(result);
        }).RequireAuthorization(PermissionPolicyProvider.For(AppPermissions.ProjectsCreate))
          .WithName("CreateProject");

        projects.MapGet("/", async (ClaimsPrincipal principal, UserManager<IdentityUser> users,
            ProjectAdministrationService service) =>
            Results.Ok(ApiResponse<IReadOnlyCollection<ProjectInfo>>.Ok(
                await service.ListAsync(users.GetUserId(principal) ?? string.Empty))))
            .WithName("ListProjects");

        projects.MapGet("/{projectId:guid}", async (Guid projectId, ClaimsPrincipal principal,
            UserManager<IdentityUser> users, ProjectAdministrationService service) =>
            ToResult(await service.GetAsync(projectId, users.GetUserId(principal) ?? string.Empty)))
            .WithName("GetProject");

        projects.MapPut("/{projectId:guid}", async (Guid projectId, UpdateProjectRequest request,
            ClaimsPrincipal principal, UserManager<IdentityUser> users, ProjectAdministrationService service) =>
            ToResult(await service.UpdateAsync(projectId, users.GetUserId(principal) ?? string.Empty, request.Name, request.Description)))
            .WithName("UpdateProject");

        projects.MapPost("/{projectId:guid}/archive", async (Guid projectId, ClaimsPrincipal principal,
            UserManager<IdentityUser> users, ProjectAdministrationService service) =>
            ToResult(await service.ArchiveAsync(projectId, users.GetUserId(principal) ?? string.Empty)))
            .WithName("ArchiveProject");

        projects.MapPost("/{projectId:guid}/restore", async (Guid projectId, ClaimsPrincipal principal,
            UserManager<IdentityUser> users, ProjectAdministrationService service) =>
            ToResult(await service.RestoreAsync(projectId, users.GetUserId(principal) ?? string.Empty)))
            .WithName("RestoreProject");

        projects.MapGet("/{projectId:guid}/members", async (Guid projectId, ClaimsPrincipal principal,
            UserManager<IdentityUser> users, ProjectAdministrationService service) =>
            ToResult(await service.GetMembersAsync(projectId, users.GetUserId(principal) ?? string.Empty)))
            .WithTags("Project Members").WithName("ListProjectMembers");

        projects.MapGet("/{projectId:guid}/member-candidates", async (Guid projectId, string? search,
            ClaimsPrincipal principal, UserManager<IdentityUser> users, ProjectAdministrationService service) =>
            ToResult(await service.SearchMemberCandidatesAsync(
                projectId, users.GetUserId(principal) ?? string.Empty, search)))
            .WithTags("Project Members").WithName("SearchProjectMemberCandidates");

        projects.MapPost("/{projectId:guid}/members", async (Guid projectId, AddProjectMemberRequest request,
            ClaimsPrincipal principal, UserManager<IdentityUser> users, ProjectAdministrationService service) =>
            ToResult(await service.AddMemberAsync(projectId, users.GetUserId(principal) ?? string.Empty, request.Email, request.Role)))
            .WithTags("Project Members").WithName("AddProjectMember");

        projects.MapPut("/{projectId:guid}/members/{userId}/role", async (Guid projectId, string userId,
            UpdateProjectMemberRoleRequest request, ClaimsPrincipal principal, UserManager<IdentityUser> users,
            ProjectAdministrationService service) =>
            ToResult(await service.ChangeMemberRoleAsync(projectId, users.GetUserId(principal) ?? string.Empty, userId, request.Role)))
            .WithTags("Project Members").WithName("UpdateProjectMemberRole");

        projects.MapDelete("/{projectId:guid}/members/{userId}", async (Guid projectId, string userId,
            ClaimsPrincipal principal, UserManager<IdentityUser> users, ProjectAdministrationService service) =>
            ToResult(await service.RemoveMemberAsync(projectId, users.GetUserId(principal) ?? string.Empty, userId)))
            .WithTags("Project Members").WithName("RemoveProjectMember");

        return endpoints;
    }

    private static IResult ToResult<T>(AdministrationResult<T> result) => result.Status switch
    {
        AdministrationResultStatus.Success => Results.Ok(ApiResponse<T>.Ok(result.Data, result.Message)),
        AdministrationResultStatus.ValidationError => Results.BadRequest(ApiResponse<T>.Fail(result.Message, ApiErrorCodes.ValidationError)),
        AdministrationResultStatus.NotFound => Results.NotFound(ApiResponse<T>.Fail(result.Message, ApiErrorCodes.NotFound)),
        AdministrationResultStatus.Forbidden => Results.Json(ApiResponse<T>.Fail(result.Message, ApiErrorCodes.Forbidden), statusCode: StatusCodes.Status403Forbidden),
        AdministrationResultStatus.Conflict => Results.Json(ApiResponse<T>.Fail(result.Message, ApiErrorCodes.Conflict), statusCode: StatusCodes.Status409Conflict),
        _ => throw new InvalidOperationException("Unknown project result status.")
    };
}
