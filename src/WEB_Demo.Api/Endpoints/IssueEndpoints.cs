using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WEB_Demo.Api.Models;
using WEB_Demo.Api.Services;

namespace WEB_Demo.Api.Endpoints;

public static class IssueEndpoints
{
    public static IEndpointRouteBuilder MapIssueEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var issues = endpoints.MapGroup("/api/projects/{projectId:guid}/issues")
            .WithTags("Issues").RequireAuthorization();

        issues.MapGet("/", async (Guid projectId, [AsParameters] IssueListQuery request,
            ClaimsPrincipal principal, UserManager<IdentityUser> users, IssueAdministrationService service) =>
            ToResult(await service.ListAsync(projectId, UserId(principal, users), request)))
            .WithName("ListProjectIssues");

        issues.MapPost("/", async (Guid projectId, CreateIssueRequest request, ClaimsPrincipal principal,
            UserManager<IdentityUser> users, IssueAdministrationService service) =>
        {
            var result = await service.CreateAsync(projectId, UserId(principal, users), request);
            return result.Status == AdministrationResultStatus.Success
                ? Results.Created($"/api/projects/{projectId}/issues/{result.Data!.Id}", ApiResponse<IssueInfo>.Ok(result.Data, "Issue created."))
                : ToResult(result);
        }).WithName("CreateProjectIssue");

        issues.MapGet("/{issueId:guid}", async (Guid projectId, Guid issueId, ClaimsPrincipal principal,
            UserManager<IdentityUser> users, IssueAdministrationService service) =>
            ToResult(await service.GetAsync(projectId, issueId, UserId(principal, users))))
            .WithName("GetProjectIssue");

        issues.MapPut("/{issueId:guid}", async (Guid projectId, Guid issueId, UpdateIssueRequest request,
            ClaimsPrincipal principal, UserManager<IdentityUser> users, IssueAdministrationService service) =>
            ToResult(await service.UpdateAsync(projectId, issueId, UserId(principal, users), request)))
            .WithName("UpdateProjectIssue");

        issues.MapPut("/{issueId:guid}/assignee", async (Guid projectId, Guid issueId, AssignIssueRequest request,
            ClaimsPrincipal principal, UserManager<IdentityUser> users, IssueAdministrationService service) =>
            ToResult(await service.AssignAsync(projectId, issueId, UserId(principal, users), request.AssigneeUserId)))
            .WithName("AssignProjectIssue");

        issues.MapPost("/{issueId:guid}/transitions", async (Guid projectId, Guid issueId, TransitionIssueRequest request,
            ClaimsPrincipal principal, UserManager<IdentityUser> users, IssueAdministrationService service) =>
            ToResult(await service.TransitionAsync(projectId, issueId, UserId(principal, users), request.Status)))
            .WithName("TransitionProjectIssue");

        issues.MapGet("/{issueId:guid}/history", async (Guid projectId, Guid issueId, ClaimsPrincipal principal,
            UserManager<IdentityUser> users, IssueAdministrationService service) =>
            ToResult(await service.GetHistoryAsync(projectId, issueId, UserId(principal, users))))
            .WithName("ListProjectIssueHistory");

        issues.MapGet("/{issueId:guid}/comments", async (Guid projectId, Guid issueId, ClaimsPrincipal principal,
            UserManager<IdentityUser> users, IssueAdministrationService service) =>
            ToResult(await service.GetCommentsAsync(projectId, issueId, UserId(principal, users))))
            .WithName("ListProjectIssueComments");

        issues.MapPost("/{issueId:guid}/comments", async (Guid projectId, Guid issueId,
            CreateIssueCommentRequest request, ClaimsPrincipal principal, UserManager<IdentityUser> users,
            IssueAdministrationService service) =>
            ToResult(await service.AddCommentAsync(projectId, issueId, UserId(principal, users), request.Content)))
            .WithName("AddProjectIssueComment");

        return endpoints;
    }

    private static string UserId(ClaimsPrincipal principal, UserManager<IdentityUser> users) =>
        users.GetUserId(principal) ?? string.Empty;

    private static IResult ToResult<T>(AdministrationResult<T> result) => result.Status switch
    {
        AdministrationResultStatus.Success => Results.Ok(ApiResponse<T>.Ok(result.Data, result.Message)),
        AdministrationResultStatus.ValidationError => Results.BadRequest(ApiResponse<T>.Fail(result.Message, ApiErrorCodes.ValidationError)),
        AdministrationResultStatus.NotFound => Results.NotFound(ApiResponse<T>.Fail(result.Message, ApiErrorCodes.NotFound)),
        AdministrationResultStatus.Forbidden => Results.Json(ApiResponse<T>.Fail(result.Message, ApiErrorCodes.Forbidden), statusCode: StatusCodes.Status403Forbidden),
        AdministrationResultStatus.Conflict => Results.Json(ApiResponse<T>.Fail(result.Message, ApiErrorCodes.Conflict), statusCode: StatusCodes.Status409Conflict),
        _ => throw new InvalidOperationException("Unknown Issue result status.")
    };
}
