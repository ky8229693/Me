using Microsoft.AspNetCore.Identity;

namespace WEB_Demo.Api.Data;

public sealed class ProjectMembership
{
    public Guid ProjectId { get; set; }
    public required string UserId { get; set; }
    public ProjectRole Role { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Project Project { get; set; } = null!;
    public IdentityUser User { get; set; } = null!;
}

