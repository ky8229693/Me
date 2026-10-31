using Microsoft.EntityFrameworkCore;
using WEB_Demo.Api.Data;
using WEB_Demo.Api.Models;

namespace WEB_Demo.Api.Services;

public sealed class DashboardService(AppDbContext dbContext)
{
    private const int ListLimit = 10;

    public async Task<DashboardInfo> GetAsync(string userId)
    {
        var activeProjectIds = dbContext.ProjectMemberships.AsNoTracking()
            .Where(item => item.UserId == userId && item.Project.Status == ProjectStatus.Active)
            .Select(item => item.ProjectId);
        var issues = dbContext.Issues.AsNoTracking()
            .Where(item => activeProjectIds.Contains(item.ProjectId));
        var now = DateTimeOffset.UtcNow;
        var assigned = issues.Where(item => item.AssigneeUserId == userId && item.Status != IssueStatus.Done);
        var overdue = assigned.Where(item => item.DueDate != null && item.DueDate < now);
        var assignedCount = await assigned.CountAsync();
        var overdueCount = await overdue.CountAsync();
        var rawCounts = await issues.GroupBy(item => item.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() }).ToArrayAsync();
        var statusCounts = Enum.GetValues<IssueStatus>()
            .Select(status => new DashboardStatusCount(status.ToString(), rawCounts.FirstOrDefault(item => item.Status == status)?.Count ?? 0))
            .ToArray();
        var assignedItems = await ProjectIssues(assigned.OrderBy(item => item.DueDate == null).ThenBy(item => item.DueDate).ThenByDescending(item => item.UpdatedAt));
        var overdueItems = await ProjectIssues(overdue.OrderBy(item => item.DueDate));
        var activity = await dbContext.IssueHistory.AsNoTracking()
            .Where(item => activeProjectIds.Contains(item.ProjectId))
            .OrderByDescending(item => item.CreatedAt).Take(ListLimit)
            .Select(item => new DashboardActivityInfo(item.ProjectId, item.Project.Name, item.IssueId,
                item.ChangeType + (item.FieldName == null ? string.Empty : " · " + item.FieldName), item.CreatedAt))
            .ToArrayAsync();
        return new DashboardInfo(assignedCount, overdueCount, statusCounts, assignedItems, overdueItems, activity);
    }

    private async Task<IReadOnlyCollection<DashboardIssueInfo>> ProjectIssues(IQueryable<Issue> query) =>
        await query.Take(ListLimit).Select(item => new DashboardIssueInfo(
            item.Id, item.ProjectId, item.Project.Name, item.Title, item.Status.ToString(), item.Priority.ToString(),
            item.DueDate, item.UpdatedAt, item.IssueLabels.OrderBy(mapping => mapping.Label.Name)
                .Select(mapping => new IssueLabelInfo(mapping.Label.Id, mapping.Label.Name, mapping.Label.Color)).ToArray()))
            .ToArrayAsync();
}
