using Microsoft.AspNetCore.Identity;

namespace WEB_Demo.Api.Data;

public sealed class ProjectAuditEvent
{
    public long Id { get; set; }
    public Guid ProjectId { get; set; }
    public required string ActorUserId { get; set; }
    public required string Action { get; set; }
    public required string TargetType { get; set; }
    public required string TargetId { get; set; }
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Project Project { get; set; } = null!;
    public IdentityUser ActorUser { get; set; } = null!;
}

