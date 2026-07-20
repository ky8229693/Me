using Microsoft.EntityFrameworkCore;
using WEB_Demo.Api.Authorization;
using WEB_Demo.Api.Data;
using WEB_Demo.Api.Models;

namespace WEB_Demo.Api.Services;

public sealed class IssueAdministrationService(AppDbContext dbContext, ProjectAccessService projectAccess)
{
    public async Task<AdministrationResult<PagedResult<IssueInfo>>> ListAsync(
        Guid projectId, string userId, IssueListQuery request)
    {
        var context = await GetContextAsync(projectId, userId);
        if (context is null) return AdministrationResult<PagedResult<IssueInfo>>.NotFound("Project was not found.");
        if (request.Page < 1 || request.PageSize is < 1 or > 100)
            return AdministrationResult<PagedResult<IssueInfo>>.Validation("Page must be at least 1 and page size must be between 1 and 100.");
        if (!TryParseOptional<IssueType>(request.Type, out var type) ||
            !TryParseOptional<IssueStatus>(request.Status, out var status) ||
            !TryParseOptional<IssuePriority>(request.Priority, out var priority))
            return AdministrationResult<PagedResult<IssueInfo>>.Validation("One or more Issue filters are invalid.");

        var query = dbContext.Issues.AsNoTracking()
            .Include(item => item.CreatorUser).Include(item => item.AssigneeUser)
            .Where(item => item.ProjectId == projectId);
        var search = request.Search?.Trim().ToLower();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(item => item.Title.ToLower().Contains(search) ||
                item.Description != null && item.Description.ToLower().Contains(search));
        if (type.HasValue) query = query.Where(item => item.Type == type.Value);
        if (status.HasValue) query = query.Where(item => item.Status == status.Value);
        if (priority.HasValue) query = query.Where(item => item.Priority == priority.Value);
        if (request.ExcludeDone) query = query.Where(item => item.Status != IssueStatus.Done);
        if (!string.IsNullOrWhiteSpace(request.AssigneeUserId))
            query = query.Where(item => item.AssigneeUserId == request.AssigneeUserId);

        var total = await query.CountAsync();
        var issues = await query.OrderByDescending(item => item.UpdatedAt)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToArrayAsync();
        var items = issues.Select(item => ToInfo(item, context.Value.Member, context.Value.Project.Status)).ToArray();
        return AdministrationResult<PagedResult<IssueInfo>>.Success(
            new PagedResult<IssueInfo>(items, request.Page, request.PageSize, total));
    }

    public async Task<AdministrationResult<IssueInfo>> GetAsync(Guid projectId, Guid issueId, string userId)
    {
        var context = await GetContextAsync(projectId, userId);
        if (context is null) return AdministrationResult<IssueInfo>.NotFound("Issue was not found.");
        var issue = await QueryIssue(projectId, issueId).SingleOrDefaultAsync();
        return issue is null
            ? AdministrationResult<IssueInfo>.NotFound("Issue was not found.")
            : AdministrationResult<IssueInfo>.Success(ToInfo(issue, context.Value.Member, context.Value.Project.Status));
    }

    public async Task<AdministrationResult<IssueInfo>> CreateAsync(
        Guid projectId, string userId, CreateIssueRequest request)
    {
        var context = await GetContextAsync(projectId, userId);
        if (context is null) return AdministrationResult<IssueInfo>.NotFound("Project was not found.");
        if (!IssueAccessService.CanCreate(context.Value.Member, context.Value.Project.Status))
            return AdministrationResult<IssueInfo>.Forbidden(context.Value.Project.Status == ProjectStatus.Archived
                ? "Archived projects are read-only." : "You cannot create Issues in this project.");
        var validation = ValidateFields(request.Title, request.Description, request.Type, request.Priority,
            out var title, out var description, out var type, out var priority);
        if (validation is not null) return AdministrationResult<IssueInfo>.Validation(validation);
        var assigneeFailure = await ValidateAssigneeAsync(projectId, context.Value.Member, request.AssigneeUserId, true);
        if (assigneeFailure is not null) return AdministrationResult<IssueInfo>.Forbidden(assigneeFailure);

        var now = DateTimeOffset.UtcNow;
        var issue = new Issue
        {
            ProjectId = projectId, Title = title!, Description = description, Type = type,
            Priority = priority, Status = IssueStatus.Draft, CreatorUserId = userId,
            AssigneeUserId = NormalizeId(request.AssigneeUserId), DueDate = request.DueDate,
            CreatedAt = now, UpdatedAt = now
        };
        dbContext.Issues.Add(issue);
        AddHistory(issue, userId, IssueHistoryChangeType.Created, null, null, issue.Title, now);
        await dbContext.SaveChangesAsync();
        return await GetAsync(projectId, issue.Id, userId);
    }

    public async Task<AdministrationResult<IssueInfo>> UpdateAsync(
        Guid projectId, Guid issueId, string userId, UpdateIssueRequest request)
    {
        var context = await GetContextAsync(projectId, userId);
        if (context is null) return AdministrationResult<IssueInfo>.NotFound("Issue was not found.");
        var issue = await dbContext.Issues.SingleOrDefaultAsync(item => item.ProjectId == projectId && item.Id == issueId);
        if (issue is null) return AdministrationResult<IssueInfo>.NotFound("Issue was not found.");
        if (!IssueAccessService.CanEdit(context.Value.Member, context.Value.Project.Status, issue))
            return AdministrationResult<IssueInfo>.Forbidden(context.Value.Project.Status == ProjectStatus.Archived
                ? "Archived projects are read-only." : "You cannot update this Issue.");
        var validation = ValidateFields(request.Title, request.Description, request.Type, request.Priority,
            out var title, out var description, out var type, out var priority);
        if (validation is not null) return AdministrationResult<IssueInfo>.Validation(validation);

        var now = DateTimeOffset.UtcNow;
        TrackChange(issue, userId, nameof(Issue.Title), issue.Title, title, now, value => issue.Title = value!);
        TrackChange(issue, userId, nameof(Issue.Description), issue.Description, description, now, value => issue.Description = value);
        TrackChange(issue, userId, nameof(Issue.Type), issue.Type.ToString(), type.ToString(), now, _ => issue.Type = type);
        TrackChange(issue, userId, nameof(Issue.Priority), issue.Priority.ToString(), priority.ToString(), now, _ => issue.Priority = priority);
        TrackChange(issue, userId, nameof(Issue.DueDate), FormatDate(issue.DueDate), FormatDate(request.DueDate), now, _ => issue.DueDate = request.DueDate);
        issue.UpdatedAt = now;
        await dbContext.SaveChangesAsync();
        return await GetAsync(projectId, issueId, userId);
    }

    public async Task<AdministrationResult<IssueInfo>> AssignAsync(
        Guid projectId, Guid issueId, string userId, string? assigneeUserId)
    {
        var context = await GetContextAsync(projectId, userId);
        if (context is null) return AdministrationResult<IssueInfo>.NotFound("Issue was not found.");
        var issue = await dbContext.Issues.SingleOrDefaultAsync(item => item.ProjectId == projectId && item.Id == issueId);
        if (issue is null) return AdministrationResult<IssueInfo>.NotFound("Issue was not found.");
        if (!IssueAccessService.CanAssign(context.Value.Member, context.Value.Project.Status))
            return AdministrationResult<IssueInfo>.Forbidden(context.Value.Project.Status == ProjectStatus.Archived
                ? "Archived projects are read-only." : "You cannot assign this Issue.");
        var failure = await ValidateAssigneeAsync(projectId, context.Value.Member, assigneeUserId, false);
        if (failure is not null) return AdministrationResult<IssueInfo>.Validation(failure);
        var normalized = NormalizeId(assigneeUserId);
        if (issue.AssigneeUserId != normalized)
        {
            var now = DateTimeOffset.UtcNow;
            AddHistory(issue, userId, IssueHistoryChangeType.AssignmentChanged, nameof(Issue.AssigneeUserId), issue.AssigneeUserId, normalized, now);
            issue.AssigneeUserId = normalized;
            issue.UpdatedAt = now;
            await dbContext.SaveChangesAsync();
        }
        return await GetAsync(projectId, issueId, userId);
    }

    public async Task<AdministrationResult<IssueInfo>> TransitionAsync(
        Guid projectId, Guid issueId, string userId, string? requestedStatus)
    {
        var context = await GetContextAsync(projectId, userId);
        if (context is null) return AdministrationResult<IssueInfo>.NotFound("Issue was not found.");
        var issue = await dbContext.Issues.SingleOrDefaultAsync(item => item.ProjectId == projectId && item.Id == issueId);
        if (issue is null) return AdministrationResult<IssueInfo>.NotFound("Issue was not found.");
        if (!Enum.TryParse<IssueStatus>(requestedStatus?.Trim(), true, out var next) || !Enum.IsDefined(next))
            return AdministrationResult<IssueInfo>.Validation("Issue status is invalid.");
        if (!IssueAccessService.AllowedTransitions(context.Value.Member, context.Value.Project.Status, issue).Contains(next))
            return AdministrationResult<IssueInfo>.Forbidden(context.Value.Project.Status == ProjectStatus.Archived
                ? "Archived projects are read-only." : "That Issue transition is not allowed.");
        var now = DateTimeOffset.UtcNow;
        AddHistory(issue, userId, IssueHistoryChangeType.StatusChanged, nameof(Issue.Status), issue.Status.ToString(), next.ToString(), now);
        issue.Status = next;
        issue.UpdatedAt = now;
        await dbContext.SaveChangesAsync();
        return await GetAsync(projectId, issueId, userId);
    }

    public async Task<AdministrationResult<IReadOnlyCollection<IssueHistoryInfo>>> GetHistoryAsync(
        Guid projectId, Guid issueId, string userId)
    {
        if (await GetContextAsync(projectId, userId) is null ||
            !await dbContext.Issues.AnyAsync(item => item.ProjectId == projectId && item.Id == issueId))
            return AdministrationResult<IReadOnlyCollection<IssueHistoryInfo>>.NotFound("Issue was not found.");
        var items = await dbContext.IssueHistory.AsNoTracking().Where(item => item.ProjectId == projectId && item.IssueId == issueId)
            .OrderBy(item => item.CreatedAt).Select(item => new IssueHistoryInfo(item.Id, item.ActorUserId,
                item.ActorUser.Email ?? string.Empty, item.ChangeType.ToString(), item.FieldName,
                item.PreviousValue, item.NewValue, item.CreatedAt)).ToArrayAsync();
        return AdministrationResult<IReadOnlyCollection<IssueHistoryInfo>>.Success(items);
    }

    public async Task<AdministrationResult<IReadOnlyCollection<IssueCommentInfo>>> GetCommentsAsync(
        Guid projectId, Guid issueId, string userId)
    {
        if (await GetContextAsync(projectId, userId) is null ||
            !await dbContext.Issues.AnyAsync(item => item.ProjectId == projectId && item.Id == issueId))
            return AdministrationResult<IReadOnlyCollection<IssueCommentInfo>>.NotFound("Issue was not found.");
        var items = await dbContext.IssueComments.AsNoTracking().Where(item => item.ProjectId == projectId && item.IssueId == issueId)
            .OrderBy(item => item.CreatedAt).Select(item => new IssueCommentInfo(item.Id, item.AuthorUserId,
                item.AuthorUser.Email ?? string.Empty, item.Content, item.CreatedAt)).ToArrayAsync();
        return AdministrationResult<IReadOnlyCollection<IssueCommentInfo>>.Success(items);
    }

    public async Task<AdministrationResult<IssueCommentInfo>> AddCommentAsync(
        Guid projectId, Guid issueId, string userId, string? content)
    {
        var context = await GetContextAsync(projectId, userId);
        if (context is null) return AdministrationResult<IssueCommentInfo>.NotFound("Issue was not found.");
        if (!IssueAccessService.CanComment(context.Value.Member, context.Value.Project.Status))
            return AdministrationResult<IssueCommentInfo>.Forbidden(context.Value.Project.Status == ProjectStatus.Archived
                ? "Archived projects are read-only." : "You cannot comment on this Issue.");
        if (!await dbContext.Issues.AnyAsync(item => item.ProjectId == projectId && item.Id == issueId))
            return AdministrationResult<IssueCommentInfo>.NotFound("Issue was not found.");
        var normalized = content?.Trim();
        if (normalized is not { Length: >= 1 and <= 5000 })
            return AdministrationResult<IssueCommentInfo>.Validation("Comment must contain between 1 and 5000 characters.");
        var comment = new IssueComment
        {
            ProjectId = projectId, IssueId = issueId, AuthorUserId = userId,
            Content = normalized, CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.IssueComments.Add(comment);
        await dbContext.SaveChangesAsync();
        var email = await dbContext.Users.Where(item => item.Id == userId).Select(item => item.Email).SingleAsync();
        return AdministrationResult<IssueCommentInfo>.Success(
            new IssueCommentInfo(comment.Id, userId, email ?? string.Empty, comment.Content, comment.CreatedAt), "Comment added.");
    }

    private async Task<(Project Project, ProjectMembership Member)?> GetContextAsync(Guid projectId, string userId)
    {
        var membership = await projectAccess.GetMembershipAsync(projectId, userId);
        if (membership is null) return null;
        var project = await dbContext.Projects.AsNoTracking().SingleOrDefaultAsync(item => item.Id == projectId);
        return project is null ? null : (project, membership);
    }

    private IQueryable<Issue> QueryIssue(Guid projectId, Guid issueId) =>
        dbContext.Issues.AsNoTracking().Include(item => item.CreatorUser).Include(item => item.AssigneeUser)
            .Where(item => item.ProjectId == projectId && item.Id == issueId);

    private async Task<string?> ValidateAssigneeAsync(
        Guid projectId, ProjectMembership actor, string? assigneeUserId, bool duringCreation)
    {
        var normalized = NormalizeId(assigneeUserId);
        if (normalized is null) return null;
        if (duringCreation && actor.Role == ProjectRole.Member && normalized != actor.UserId)
            return "Members may only assign a new Issue to themselves.";
        return await dbContext.ProjectMemberships.AnyAsync(item => item.ProjectId == projectId && item.UserId == normalized)
            ? null : "Assignee must be a current project member.";
    }

    private static string? ValidateFields(string? rawTitle, string? rawDescription, string? rawType, string? rawPriority,
        out string? title, out string? description, out IssueType type, out IssuePriority priority)
    {
        title = rawTitle?.Trim();
        description = string.IsNullOrWhiteSpace(rawDescription) ? null : rawDescription.Trim();
        if (title is not { Length: >= 2 and <= 200 }) { type = default; priority = default; return "Title must contain between 2 and 200 characters."; }
        if (description is { Length: > 10000 }) { type = default; priority = default; return "Description cannot exceed 10000 characters."; }
        if (!Enum.TryParse(rawType?.Trim(), true, out type) || !Enum.IsDefined(type)) { priority = default; return "Issue type is invalid."; }
        if (!Enum.TryParse(rawPriority?.Trim(), true, out priority) || !Enum.IsDefined(priority)) return "Issue priority is invalid.";
        return null;
    }

    private static bool TryParseOptional<T>(string? value, out T? parsed) where T : struct, Enum
    {
        parsed = null;
        if (string.IsNullOrWhiteSpace(value)) return true;
        if (!Enum.TryParse<T>(value.Trim(), true, out var result) || !Enum.IsDefined(result)) return false;
        parsed = result;
        return true;
    }

    private void TrackChange(Issue issue, string actor, string field, string? previous, string? next,
        DateTimeOffset now, Action<string?> apply)
    {
        if (previous == next) return;
        AddHistory(issue, actor, IssueHistoryChangeType.FieldChanged, field, previous, next, now);
        apply(next);
    }

    private void AddHistory(Issue issue, string actor, IssueHistoryChangeType type, string? field,
        string? previous, string? next, DateTimeOffset now) => dbContext.IssueHistory.Add(new IssueHistory
        {
            ProjectId = issue.ProjectId, IssueId = issue.Id, ActorUserId = actor,
            ChangeType = type, FieldName = field, PreviousValue = previous, NewValue = next, CreatedAt = now
        });

    private static IssueInfo ToInfo(Issue issue, ProjectMembership member, ProjectStatus projectStatus) =>
        new(issue.Id, issue.ProjectId, issue.Title, issue.Description, issue.Type.ToString(), issue.Priority.ToString(),
            issue.Status.ToString(), issue.CreatorUserId, issue.CreatorUser.Email ?? string.Empty,
            issue.AssigneeUserId, issue.AssigneeUser?.Email, issue.DueDate, issue.CreatedAt, issue.UpdatedAt,
            IssueAccessService.CanEdit(member, projectStatus, issue), IssueAccessService.CanAssign(member, projectStatus),
            IssueAccessService.CanComment(member, projectStatus),
            IssueAccessService.AllowedTransitions(member, projectStatus, issue).Select(item => item.ToString()).ToArray());

    private static string? NormalizeId(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? FormatDate(DateTimeOffset? value) => value?.ToUniversalTime().ToString("O");
}
