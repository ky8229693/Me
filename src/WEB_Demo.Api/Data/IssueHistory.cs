using Microsoft.AspNetCore.Identity;

namespace WEB_Demo.Api.Data;

public enum IssueHistoryChangeType
{
    Created,
    FieldChanged,
    StatusChanged,
    AssignmentChanged
}

public sealed class IssueHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid IssueId { get; set; }
    public required string ActorUserId { get; set; }
    public IssueHistoryChangeType ChangeType { get; set; }
    public string? FieldName { get; set; }
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Project Project { get; set; } = null!;
    public Issue Issue { get; set; } = null!;
    public IdentityUser ActorUser { get; set; } = null!;
}
