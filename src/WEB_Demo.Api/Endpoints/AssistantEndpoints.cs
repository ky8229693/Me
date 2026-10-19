using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using WEB_Demo.Api.Models;
using WEB_Demo.Api.Services;

namespace WEB_Demo.Api.Endpoints;

public static class AssistantEndpoints
{
    public static IEndpointRouteBuilder MapAssistantEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/assistant").WithTags("Assistant").RequireAuthorization();
        group.MapPost("/conversations", CreateAsync).WithName("CreateAssistantConversation");
        group.MapGet("/conversations", ListAsync).WithName("ListAssistantConversations");
        group.MapGet("/conversations/{conversationId:guid}", GetAsync).WithName("GetAssistantConversation");
        group.MapPut("/conversations/{conversationId:guid}/context", UpdateContextAsync).WithName("UpdateAssistantContext");
        group.MapPost("/conversations/{conversationId:guid}/messages", SendMessageAsync).WithName("SendAssistantMessage");
        group.MapPost("/conversations/{conversationId:guid}/actions/{actionId:guid}/confirm", ConfirmAsync)
            .WithName("ConfirmAssistantAction");
        group.MapPost("/conversations/{conversationId:guid}/actions/{actionId:guid}/cancel", CancelAsync)
            .WithName("CancelAssistantAction");
        return endpoints;
    }

    private static string UserId(ClaimsPrincipal principal, UserManager<IdentityUser> users) =>
        users.GetUserId(principal) ?? string.Empty;

    private static async Task<IResult> CreateAsync(CreateAssistantConversationRequest request,
        ClaimsPrincipal principal, UserManager<IdentityUser> users, AssistantConversationService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.CreateAsync(UserId(principal, users), request.Title,
                request.ProjectId, request.IssueId, cancellationToken);
            return Results.Created($"/api/assistant/conversations/{result.Id}", ApiResponse<AssistantConversationInfo>.Ok(result));
        }
        catch (AssistantContextException exception)
        {
            return Results.NotFound(ApiResponse<object>.Fail(exception.Message, "ASSISTANT_CONTEXT_NOT_FOUND"));
        }
    }

    private static async Task<IResult> ListAsync(ClaimsPrincipal principal, UserManager<IdentityUser> users,
        AssistantConversationService service, CancellationToken cancellationToken) =>
        Results.Ok(ApiResponse<IReadOnlyCollection<AssistantConversationSummary>>.Ok(
            await service.ListAsync(UserId(principal, users), cancellationToken)));

    private static async Task<IResult> GetAsync(Guid conversationId, ClaimsPrincipal principal,
        UserManager<IdentityUser> users, AssistantConversationService service, CancellationToken cancellationToken)
    {
        var result = await service.LoadAsync(conversationId, UserId(principal, users), cancellationToken);
        return result is null ? Results.NotFound(ApiResponse<object>.Fail("Conversation was not found.", "ASSISTANT_NOT_FOUND"))
            : Results.Ok(ApiResponse<AssistantConversationInfo>.Ok(result));
    }

    private static async Task<IResult> UpdateContextAsync(Guid conversationId, UpdateAssistantContextRequest request,
        ClaimsPrincipal principal, UserManager<IdentityUser> users, AssistantConversationService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.UpdateContextAsync(conversationId, UserId(principal, users),
                request.ProjectId, request.IssueId, cancellationToken);
            return result is null ? Results.NotFound(ApiResponse<object>.Fail("Conversation was not found.", "ASSISTANT_NOT_FOUND"))
                : Results.Ok(ApiResponse<AssistantConversationInfo>.Ok(result));
        }
        catch (AssistantContextException exception)
        {
            return Results.NotFound(ApiResponse<object>.Fail(exception.Message, "ASSISTANT_CONTEXT_NOT_FOUND"));
        }
    }

    private static async Task<IResult> SendMessageAsync(Guid conversationId, SendAssistantMessageRequest request,
        ClaimsPrincipal principal, UserManager<IdentityUser> users, AssistantOrchestrationService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.SendAsync(conversationId, UserId(principal, users), request.Message, cancellationToken);
            return result is null ? Results.NotFound(ApiResponse<object>.Fail("Conversation was not found.", "ASSISTANT_NOT_FOUND"))
                : Results.Ok(ApiResponse<AssistantTurnResponse>.Ok(result));
        }
        catch (AssistantMessageValidationException exception)
        {
            return Results.BadRequest(ApiResponse<object>.Fail(exception.Message, "ASSISTANT_INVALID_MESSAGE"));
        }
        catch (SpecAssistantUnavailableException)
        {
            return Results.Json(ApiResponse<object>.Fail("Assistant is temporarily unavailable or not configured.",
                "ASSISTANT_UNAVAILABLE"), statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    private static async Task<IResult> ConfirmAsync(Guid conversationId, Guid actionId,
        ClaimsPrincipal principal, UserManager<IdentityUser> users, AssistantConfirmationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ConfirmAsync(conversationId, actionId, UserId(principal, users), cancellationToken);
        return result.Status switch
        {
            AdministrationResultStatus.Success => Results.Ok(ApiResponse<AssistantConfirmationResponse>.Ok(result.Data!, result.Message)),
            AdministrationResultStatus.NotFound => Results.NotFound(ApiResponse<object>.Fail(result.Message, "ASSISTANT_ACTION_NOT_FOUND")),
            AdministrationResultStatus.Forbidden => Results.Json(ApiResponse<object>.Fail(result.Message, ApiErrorCodes.Forbidden), statusCode: 403),
            AdministrationResultStatus.ValidationError => Results.BadRequest(ApiResponse<object>.Fail(result.Message, ApiErrorCodes.ValidationError)),
            _ => Results.Conflict(ApiResponse<object>.Fail(result.Message, ApiErrorCodes.Conflict))
        };
    }

    private static async Task<IResult> CancelAsync(Guid conversationId, Guid actionId,
        ClaimsPrincipal principal, UserManager<IdentityUser> users, AssistantConversationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CancelAsync(conversationId, actionId, UserId(principal, users), cancellationToken);
        return result is null ? Results.NotFound(ApiResponse<object>.Fail("Pending action was not found.", "ASSISTANT_ACTION_NOT_FOUND"))
            : Results.Ok(ApiResponse<AssistantPendingActionInfo>.Ok(result));
    }
}
