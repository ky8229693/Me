namespace WEB_Demo.Api.Data;

public enum ProjectStatus
{
    Active,
    Archived
}

public enum ProjectRole
{
    Owner,
    Manager,
    Member,
    Viewer
}

public sealed class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public string? Description { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Active;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<ProjectMembership> Memberships { get; set; } = [];
    public ICollection<ProjectAuditEvent> AuditEvents { get; set; } = [];
    public ICollection<Issue> Issues { get; set; } = [];
    public ICollection<IssueHistory> IssueHistory { get; set; } = [];
    public ICollection<IssueComment> IssueComments { get; set; } = [];
    public ICollection<ProjectLabel> Labels { get; set; } = [];
}
