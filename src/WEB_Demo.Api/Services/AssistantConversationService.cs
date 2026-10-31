using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WEB_Demo.Api.Data;
using WEB_Demo.Api.Authorization;
using WEB_Demo.Api.Models;
using WEB_Demo.Api.Options;

namespace WEB_Demo.Api.Services;

public sealed class AssistantConversationService(
    AppDbContext db, ProjectAccessService projectAccess,
    IOptions<SpecAssistantOptions> options, TimeProvider clock)
{
    private readonly SpecAssistantOptions settings = options.Value;

    public async Task<AssistantConversationInfo> CreateAsync(
        string userId, string? title, Guid? projectId, Guid? issueId, CancellationToken cancellationToken)
    {
        await ValidateContextAsync(userId, projectId, issueId, cancellationToken);
        var now = clock.GetUtcNow();
        var conversation = new AssistantConversation
        {
            UserId = userId,
            Title = NormalizeTitle(title),
            ContextProjectId = projectId,
            ContextIssueId = issueId,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.AssistantConversations.Add(conversation);
        await db.SaveChangesAsync(cancellationToken);
        return ToInfo(conversation, [], null);
    }

    public async Task<IReadOnlyCollection<AssistantConversationSummary>> ListAsync(
        string userId, CancellationToken cancellationToken) =>
        await db.AssistantConversations.AsNoTracking().Where(item => item.UserId == userId)
            .OrderByDescending(item => item.UpdatedAt).Take(50)
            .Select(item => new AssistantConversationSummary(item.Id, item.Title,
                new AssistantContextInfo(item.ContextProjectId, item.ContextIssueId),
                item.CreatedAt, item.UpdatedAt)).ToArrayAsync(cancellationToken);

    public async Task<AssistantConversationInfo?> LoadAsync(
        Guid id, string userId, CancellationToken cancellationToken)
    {
        var conversation = await db.AssistantConversations.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);
        if (conversation is null) return null;
        var messages = await db.AssistantMessages.AsNoTracking()
            .Where(item => item.ConversationId == id).OrderBy(item => item.CreatedAt)
            .Select(item => new AssistantMessageInfo(item.Id, item.Role.ToString(), item.Content, item.CreatedAt))
            .ToArrayAsync(cancellationToken);
        var pending = await ActivePendingQuery(id, userId).AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        return ToInfo(conversation, messages, ToPendingInfo(pending));
    }

    public async Task<AssistantConversation?> FindOwnedAsync(
        Guid id, string userId, CancellationToken cancellationToken) =>
        await db.AssistantConversations.SingleOrDefaultAsync(
            item => item.Id == id && item.UserId == userId, cancellationToken);

    public async Task<AssistantMessageInfo> AppendAsync(
        AssistantConversation conversation, AssistantMessageRole role, string content,
        string? metadataJson, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var message = new AssistantMessage
        {
            ConversationId = conversation.Id, Role = role, Content = content,
            MetadataJson = metadataJson, CreatedAt = now
        };
        conversation.UpdatedAt = now;
        db.AssistantMessages.Add(message);
        await db.SaveChangesAsync(cancellationToken);
        return new AssistantMessageInfo(message.Id, role.ToString(), content, now);
    }

    public async Task<IReadOnlyCollection<AssistantMessage>> GetModelHistoryAsync(
        Guid conversationId, CancellationToken cancellationToken) =>
        await db.AssistantMessages.AsNoTracking().Where(item => item.ConversationId == conversationId)
            .OrderByDescending(item => item.CreatedAt).Take(settings.AssistantHistoryMessages)
            .OrderBy(item => item.CreatedAt).ToArrayAsync(cancellationToken);

    public async Task<AssistantConversationInfo?> UpdateContextAsync(
        Guid id, string userId, Guid? projectId, Guid? issueId, CancellationToken cancellationToken)
    {
        var conversation = await FindOwnedAsync(id, userId, cancellationToken);
        if (conversation is null) return null;
        await ValidateContextAsync(userId, projectId, issueId, cancellationToken);
        conversation.ContextProjectId = projectId;
        conversation.ContextIssueId = issueId;
        conversation.UpdatedAt = clock.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
        return await LoadAsync(id, userId, cancellationToken);
    }

    public async Task<AssistantPendingActionInfo> PrepareAsync<T>(
        AssistantConversation conversation, AssistantPendingActionType type, T payload, object preview,
        CancellationToken cancellationToken)
    {
        await ExpirePendingAsync(conversation.Id, conversation.UserId, cancellationToken);
        if (await ActivePendingQuery(conversation.Id, conversation.UserId).AnyAsync(cancellationToken))
            throw new AssistantPendingActionConflictException();
        var now = clock.GetUtcNow();
        var action = new AssistantPendingAction
        {
            ConversationId = conversation.Id, UserId = conversation.UserId, Type = type,
            PayloadJson = JsonSerializer.Serialize(payload), PreviewJson = JsonSerializer.Serialize(preview),
            IdempotencyKey = $"{conversation.Id:N}:{Guid.NewGuid():N}",
            CreatedAt = now, ExpiresAt = now.AddMinutes(settings.AssistantPendingActionMinutes)
        };
        db.AssistantPendingActions.Add(action);
        await db.SaveChangesAsync(cancellationToken);
        return ToPendingInfo(action)!;
    }

    public async Task<AssistantPendingAction?> FindActionAsync(
        Guid conversationId, Guid actionId, string userId, CancellationToken cancellationToken) =>
        await db.AssistantPendingActions.SingleOrDefaultAsync(item => item.Id == actionId &&
            item.ConversationId == conversationId && item.UserId == userId, cancellationToken);

    public async Task<AssistantPendingActionInfo?> CancelAsync(
        Guid conversationId, Guid actionId, string userId, CancellationToken cancellationToken)
    {
        var action = await FindActionAsync(conversationId, actionId, userId, cancellationToken);
        if (action is null) return null;
        if (action.Status == AssistantPendingActionStatus.Pending)
        {
            action.Status = AssistantPendingActionStatus.Cancelled;
            action.CompletedAt = clock.GetUtcNow();
            await db.SaveChangesAsync(cancellationToken);
        }
        return ToPendingInfo(action);
    }

    public static AssistantPendingActionInfo? ToPendingInfo(AssistantPendingAction? action)
    {
        if (action is null) return null;
        using var preview = JsonDocument.Parse(action.PreviewJson);
        JsonElement? result = null;
        if (action.ResultJson is not null)
        {
            using var resultDocument = JsonDocument.Parse(action.ResultJson);
            result = resultDocument.RootElement.Clone();
        }
        return new AssistantPendingActionInfo(action.Id, action.Type.ToString(), action.Status.ToString(),
            preview.RootElement.Clone(), action.ExpiresAt, result);
    }

    private IQueryable<AssistantPendingAction> ActivePendingQuery(Guid conversationId, string userId) =>
        db.AssistantPendingActions.Where(item => item.ConversationId == conversationId && item.UserId == userId &&
            (item.Status == AssistantPendingActionStatus.Pending || item.Status == AssistantPendingActionStatus.Processing));

    private async Task ExpirePendingAsync(Guid conversationId, string userId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var expired = await ActivePendingQuery(conversationId, userId)
            .Where(item => item.ExpiresAt <= now).ToArrayAsync(cancellationToken);
        foreach (var action in expired) action.Status = AssistantPendingActionStatus.Expired;
        if (expired.Length > 0) await db.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateContextAsync(
        string userId, Guid? projectId, Guid? issueId, CancellationToken cancellationToken)
    {
        if (issueId is not null && projectId is null)
            throw new AssistantContextException("Issue context requires a project.");
        if (projectId is null) return;
        if (await projectAccess.GetMembershipAsync(projectId.Value, userId) is null)
            throw new AssistantContextException("Project context was not found.");
        if (issueId is not null && !await db.Issues.AsNoTracking().AnyAsync(
                item => item.Id == issueId && item.ProjectId == projectId, cancellationToken))
            throw new AssistantContextException("Issue context was not found.");
    }

    private static string? NormalizeTitle(string? title) =>
        string.IsNullOrWhiteSpace(title) ? null : title.Trim()[..Math.Min(title.Trim().Length, 120)];

    private static AssistantConversationInfo ToInfo(AssistantConversation conversation,
        IReadOnlyCollection<AssistantMessageInfo> messages, AssistantPendingActionInfo? pending) =>
        new(conversation.Id, conversation.Title,
            new AssistantContextInfo(conversation.ContextProjectId, conversation.ContextIssueId),
            messages, pending, conversation.CreatedAt, conversation.UpdatedAt);
}

public sealed class AssistantContextException(string message) : Exception(message);
public sealed class AssistantPendingActionConflictException : Exception
{
    public AssistantPendingActionConflictException() : base("Confirm or cancel the existing action first.") { }
}
