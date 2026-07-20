namespace WEB_Demo.Api.Models;

public sealed record CreateIssueRequest(
    string? Title, string? Description, string? Type, string? Priority,
    string? AssigneeUserId, DateTimeOffset? DueDate);

public sealed record UpdateIssueRequest(
    string? Title, string? Description, string? Type, string? Priority, DateTimeOffset? DueDate);

public sealed record AssignIssueRequest(string? AssigneeUserId);
public sealed record TransitionIssueRequest(string? Status);
public sealed record CreateIssueCommentRequest(string? Content);

public sealed record IssueListQuery(
    string? Search, string? Type, string? Status, string? Priority,
    string? AssigneeUserId, bool ExcludeDone = false, int Page = 1, int PageSize = 20);

public sealed record IssueInfo(
    Guid Id, Guid ProjectId, string Title, string? Description,
    string Type, string Priority, string Status,
    string CreatorUserId, string CreatorEmail,
    string? AssigneeUserId, string? AssigneeEmail,
    DateTimeOffset? DueDate, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt,
    bool CanEdit, bool CanAssign, bool CanComment,
    IReadOnlyCollection<string> AllowedTransitions);

public sealed record IssueHistoryInfo(
    Guid Id, string ActorUserId, string ActorEmail, string ChangeType,
    string? FieldName, string? PreviousValue, string? NewValue, DateTimeOffset CreatedAt);

public sealed record IssueCommentInfo(
    Guid Id, string AuthorUserId, string AuthorEmail, string Content, DateTimeOffset CreatedAt);
