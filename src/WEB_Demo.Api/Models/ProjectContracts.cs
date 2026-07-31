namespace WEB_Demo.Api.Models;

public sealed record CreateProjectRequest(string? Name, string? Description);
public sealed record UpdateProjectRequest(string? Name, string? Description);
public sealed record AddProjectMemberRequest(string? Email, string? Role);
public sealed record UpdateProjectMemberRoleRequest(string? Role);

public sealed record ProjectInfo(
    Guid Id,
    string Name,
    string? Description,
    string Status,
    string CurrentUserRole,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ProjectMemberInfo(
    string UserId,
    string Email,
    string Role,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ProjectMemberCandidate(string UserId, string Email);
