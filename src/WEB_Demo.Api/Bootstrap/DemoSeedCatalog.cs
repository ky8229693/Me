using WEB_Demo.Api.Data;

namespace WEB_Demo.Api.Bootstrap;

public static class DemoSeedCatalog
{
    public static readonly Guid ProjectId = Guid.Parse("d0000000-0000-4000-8000-000000000001");
    public const string ProjectName = "WEB_Demo Development";

    public static readonly IReadOnlyCollection<DemoLabelSeed> Labels =
    [
        new(Guid.Parse("d1000000-0000-4000-8000-000000000001"), "Backend", "#2563EB"),
        new(Guid.Parse("d1000000-0000-4000-8000-000000000002"), "Frontend", "#7C3AED"),
        new(Guid.Parse("d1000000-0000-4000-8000-000000000003"), "Security", "#DC2626"),
        new(Guid.Parse("d1000000-0000-4000-8000-000000000004"), "DevOps", "#EA580C"),
        new(Guid.Parse("d1000000-0000-4000-8000-000000000005"), "Testing", "#16A34A"),
        new(Guid.Parse("d1000000-0000-4000-8000-000000000006"), "Documentation", "#0891B2")
    ];

    public static readonly IReadOnlyCollection<DemoIssueSeed> Issues =
    [
        new(Guid.Parse("d2000000-0000-4000-8000-000000000001"), "API foundation", "Minimal API modules, error handling, CORS and health checks.", IssueType.Feature, IssuePriority.High, IssueStatus.Done, -40, -25, ["Backend"]),
        new(Guid.Parse("d2000000-0000-4000-8000-000000000002"), "Authentication and permission-based RBAC", "Identity, JWT rotation, session allowlist and permission policies.", IssueType.Feature, IssuePriority.Critical, IssueStatus.Done, -32, -18, ["Backend", "Security"]),
        new(Guid.Parse("d2000000-0000-4000-8000-000000000003"), "Project and Issue management", "Project memberships, Issue workflow, comments and append-only history.", IssueType.Feature, IssuePriority.High, IssueStatus.Done, -22, -10, ["Backend", "Frontend"]),
        new(Guid.Parse("d2000000-0000-4000-8000-000000000004"), "Kanban board and Project labels", "Deliver route-backed List/Board views and Project-scoped labels.", IssueType.Feature, IssuePriority.High, IssueStatus.QA, -8, 2, ["Frontend", "Backend", "Testing"]),
        new(Guid.Parse("d2000000-0000-4000-8000-000000000005"), "Dashboard workbench", "Show assigned work, overdue Issues, status counts and recent activity.", IssueType.Feature, IssuePriority.High, IssueStatus.InProgress, -6, -1, ["Frontend", "Backend"]),
        new(Guid.Parse("d2000000-0000-4000-8000-000000000006"), "Development demo seed data", "Use WEB_Demo itself as an idempotent local demonstration Project.", IssueType.ChangeRequest, IssuePriority.Medium, IssueStatus.Draft, -1, 5, ["Backend", "Documentation"]),
        new(Guid.Parse("d2000000-0000-4000-8000-000000000007"), "Structured application logging", "Add searchable request and business context without exposing secrets.", IssueType.ChangeRequest, IssuePriority.Medium, IssueStatus.Approved, 0, 14, ["Backend", "DevOps"]),
        new(Guid.Parse("d2000000-0000-4000-8000-000000000008"), "Production container verification", "Validate gateway, API image and local production operations in CI.", IssueType.Bug, IssuePriority.High, IssueStatus.QA, -4, 3, ["DevOps", "Testing"])
    ];

    public static readonly Guid CommentId = Guid.Parse("d3000000-0000-4000-8000-000000000001");
    public static readonly Guid HistoryId = Guid.Parse("d4000000-0000-4000-8000-000000000001");
}

public sealed record DemoLabelSeed(Guid Id, string Name, string Color);
public sealed record DemoIssueSeed(Guid Id, string Title, string Description, IssueType Type,
    IssuePriority Priority, IssueStatus Status, int CreatedDays, int DueDays,
    IReadOnlyCollection<string> Labels);
