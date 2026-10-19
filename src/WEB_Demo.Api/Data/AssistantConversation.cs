namespace WEB_Demo.Api.Data;

public enum AssistantMessageRole
{
    User,
    Assistant,
    Tool
}

public enum AssistantPendingActionType
{
    CreateProject,
    CreateIssue
}

public enum AssistantPendingActionStatus
{
    Pending,
    Processing,
    Completed,
    Cancelled,
    Expired,
    Failed
}

public sealed class AssistantConversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string UserId { get; set; }
    public string? Title { get; set; }
    public Guid? ContextProjectId { get; set; }
    public Guid? ContextIssueId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<AssistantMessage> Messages { get; set; } = [];
    public ICollection<AssistantPendingAction> PendingActions { get; set; } = [];
}

public sealed class AssistantMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public AssistantMessageRole Role { get; set; }
    public required string Content { get; set; }
    public string? MetadataJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public AssistantConversation Conversation { get; set; } = null!;
}

public sealed class AssistantPendingAction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public required string UserId { get; set; }
    public AssistantPendingActionType Type { get; set; }
    public AssistantPendingActionStatus Status { get; set; } = AssistantPendingActionStatus.Pending;
    public required string PayloadJson { get; set; }
    public required string PreviewJson { get; set; }
    public required string IdempotencyKey { get; set; }
    public string? ResultJson { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public uint Version { get; set; }
    public AssistantConversation Conversation { get; set; } = null!;
}
