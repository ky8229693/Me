using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using WEB_Demo.Api.Authorization;
using WEB_Demo.Api.Data;
using WEB_Demo.Api.Models;

namespace WEB_Demo.Api.Services;

public sealed record AssistantConfirmationResult(
    AdministrationResultStatus Status, AssistantConfirmationResponse? Data, string Message);

public sealed class AssistantConfirmationService(
    AppDbContext db, AssistantConversationService conversations,
    ProjectAdministrationService projects, IssueAdministrationService issues,
    UserManager<IdentityUser> users, IPermissionResolver permissions, TimeProvider clock)
{
    public async Task<AssistantConfirmationResult> ConfirmAsync(
        Guid conversationId, Guid actionId, string userId, CancellationToken cancellationToken)
    {
        var action = await conversations.FindActionAsync(conversationId, actionId, userId, cancellationToken);
        if (action is null)
            return new(AdministrationResultStatus.NotFound, null, "Pending action was not found.");
        if (action.Status == AssistantPendingActionStatus.Completed)
            return Completed(action, "Action was already completed.");
        if (action.Status != AssistantPendingActionStatus.Pending)
            return new(AdministrationResultStatus.Conflict, null, $"Action is {action.Status}.");
        if (action.ExpiresAt <= clock.GetUtcNow())
        {
            action.Status = AssistantPendingActionStatus.Expired;
            await db.SaveChangesAsync(cancellationToken);
            return new(AdministrationResultStatus.Conflict, null, "Pending action has expired.");
        }

        action.Status = AssistantPendingActionStatus.Processing;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            db.Entry(action).State = EntityState.Detached;
            var current = await conversations.FindActionAsync(conversationId, actionId, userId, cancellationToken);
            return current?.Status == AssistantPendingActionStatus.Completed
                ? Completed(current, "Action was already completed.")
                : new(AdministrationResultStatus.Conflict, null, "Action is already being processed.");
        }

        AdministrationResultStatus status;
        string message;
        object? resource;
        switch (action.Type)
        {
            case AssistantPendingActionType.CreateProject:
            {
                var user = await users.FindByIdAsync(userId);
                IReadOnlyCollection<string> granted = user is null ? [] : await permissions.GetForUserAsync(user);
                if (!granted.Contains(AppPermissions.ProjectsCreate, StringComparer.Ordinal))
                {
                    status = AdministrationResultStatus.Forbidden;
                    message = "You do not have permission to create projects.";
                    resource = null;
                    break;
                }
                var payload = JsonSerializer.Deserialize<CreateProjectActionPayload>(action.PayloadJson)!;
                var result = await projects.CreateAsync(userId, payload.Name, payload.Description);
                status = result.Status; message = result.Message; resource = result.Data;
                break;
            }
            case AssistantPendingActionType.CreateIssue:
            {
                var payload = JsonSerializer.Deserialize<CreateIssueActionPayload>(action.PayloadJson)!;
                var request = new CreateIssueRequest(payload.Title, payload.Description, payload.Type,
                    payload.Priority, payload.AssigneeUserId, payload.DueDate);
                var result = await issues.CreateAsync(payload.ProjectId, userId, request);
                status = result.Status; message = result.Message; resource = result.Data;
                break;
            }
            default:
                status = AdministrationResultStatus.ValidationError;
                message = "Unsupported pending action.";
                resource = null;
                break;
        }

        action.Status = status == AdministrationResultStatus.Success
            ? AssistantPendingActionStatus.Completed : AssistantPendingActionStatus.Failed;
        action.CompletedAt = clock.GetUtcNow();
        action.ResultJson = JsonSerializer.Serialize(new { status = status.ToString(), message, resource });
        await db.SaveChangesAsync(cancellationToken);
        var response = new AssistantConfirmationResponse(
            AssistantConversationService.ToPendingInfo(action)!, message, resource);
        return new(status, response, message);
    }

    private static AssistantConfirmationResult Completed(AssistantPendingAction action, string fallbackMessage)
    {
        if (action.ResultJson is null)
            return new(AdministrationResultStatus.Success,
                new AssistantConfirmationResponse(AssistantConversationService.ToPendingInfo(action)!, fallbackMessage, null),
                fallbackMessage);
        using var document = JsonDocument.Parse(action.ResultJson);
        var root = document.RootElement;
        var message = root.TryGetProperty("message", out var storedMessage)
            ? storedMessage.GetString() ?? fallbackMessage : fallbackMessage;
        object? resource = root.TryGetProperty("resource", out var storedResource)
            ? storedResource.Clone() : null;
        return new(AdministrationResultStatus.Success,
            new AssistantConfirmationResponse(AssistantConversationService.ToPendingInfo(action)!, message, resource), message);
    }
}
