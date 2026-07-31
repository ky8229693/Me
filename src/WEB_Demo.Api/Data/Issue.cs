using Microsoft.AspNetCore.Identity;

namespace WEB_Demo.Api.Data;

public enum IssueType
{
    Feature,
    Bug,
    ChangeRequest
}

public enum IssuePriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum IssueStatus
{
    Draft,
    Approved,
    InProgress,
    QA,
    Done
}

public sealed class Issue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public IssueType Type { get; set; }
    public IssuePriority Priority { get; set; }
    public IssueStatus Status { get; set; } = IssueStatus.Draft;
    public required string CreatorUserId { get; set; }
    public string? AssigneeUserId { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Project Project { get; set; } = null!;
    public IdentityUser CreatorUser { get; set; } = null!;
    public IdentityUser? AssigneeUser { get; set; }
    public ICollection<IssueHistory> History { get; set; } = [];
    public ICollection<IssueComment> Comments { get; set; } = [];
}
