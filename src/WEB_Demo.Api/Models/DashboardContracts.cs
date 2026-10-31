namespace WEB_Demo.Api.Models;

public sealed record DashboardIssueInfo(
    Guid Id, Guid ProjectId, string ProjectName, string Title, string Status,
    string Priority, DateTimeOffset? DueDate, DateTimeOffset UpdatedAt,
    IReadOnlyCollection<IssueLabelInfo> Labels);

public sealed record DashboardStatusCount(string Status, int Count);

public sealed record DashboardActivityInfo(
    Guid ProjectId, string ProjectName, Guid? IssueId, string Description, DateTimeOffset CreatedAt);

public sealed record DashboardInfo(
    int AssignedCount, int OverdueCount,
    IReadOnlyCollection<DashboardStatusCount> StatusCounts,
    IReadOnlyCollection<DashboardIssueInfo> AssignedIssues,
    IReadOnlyCollection<DashboardIssueInfo> OverdueIssues,
    IReadOnlyCollection<DashboardActivityInfo> RecentActivity);
