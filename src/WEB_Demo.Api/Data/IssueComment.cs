using Microsoft.AspNetCore.Identity;

namespace WEB_Demo.Api.Data;

public sealed class IssueComment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid IssueId { get; set; }
    public required string AuthorUserId { get; set; }
    public required string Content { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Project Project { get; set; } = null!;
    public Issue Issue { get; set; } = null!;
    public IdentityUser AuthorUser { get; set; } = null!;
}
