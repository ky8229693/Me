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

public sealed record ProjectOverviewStatusCount(string Status, int Count);

public sealed record ProjectOverviewIssueInfo(
    Guid Id,
    string Title,
    string Status,
    string Priority,
    DateTimeOffset? DueDate,
    DateTimeOffset UpdatedAt);

public sealed record ProjectOverviewInfo(
    int TotalCount,
    int InProgressCount,
    int CompletedCount,
    int OverdueCount,
    int CompletionRate,
    IReadOnlyCollection<ProjectOverviewStatusCount> StatusCounts,
    IReadOnlyCollection<ProjectOverviewIssueInfo> RecentIssues,
    IReadOnlyCollection<ProjectOverviewIssueInfo> UpcomingIssues);
