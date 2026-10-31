using System.Text.Json;

namespace WEB_Demo.Api.Models;

public sealed record CreateAssistantConversationRequest(string? Title, Guid? ProjectId, Guid? IssueId);
public sealed record UpdateAssistantContextRequest(Guid? ProjectId, Guid? IssueId);
public sealed record SendAssistantMessageRequest(string? Message);

public sealed record AssistantContextInfo(Guid? ProjectId, Guid? IssueId);
public sealed record AssistantMessageInfo(Guid Id, string Role, string Content, DateTimeOffset CreatedAt);
public sealed record AssistantPendingActionInfo(
    Guid Id, string Type, string Status, JsonElement Preview, DateTimeOffset ExpiresAt, JsonElement? Result);
public sealed record AssistantConversationSummary(
    Guid Id, string? Title, AssistantContextInfo Context, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record AssistantConversationInfo(
    Guid Id, string? Title, AssistantContextInfo Context,
    IReadOnlyCollection<AssistantMessageInfo> Messages,
    AssistantPendingActionInfo? PendingAction,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record AssistantTurnResponse(
    AssistantMessageInfo Message, AssistantPendingActionInfo? PendingAction,
    IReadOnlyCollection<string> Sources, string State);
public sealed record AssistantConfirmationResponse(
    AssistantPendingActionInfo Action, string Message, object? Resource);

public sealed record CreateProjectActionPayload(string Name, string? Description);
public sealed record CreateIssueActionPayload(
    Guid ProjectId, string Title, string? Description, string Type, string Priority,
    string? AssigneeUserId, DateTimeOffset? DueDate);
