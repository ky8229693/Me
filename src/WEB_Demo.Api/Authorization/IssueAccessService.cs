using WEB_Demo.Api.Data;

namespace WEB_Demo.Api.Authorization;

public static class IssueAccessService
{
    public static bool CanCreate(ProjectMembership member, ProjectStatus projectStatus) =>
        projectStatus == ProjectStatus.Active && member.Role is not ProjectRole.Viewer;

    public static bool CanEdit(ProjectMembership member, ProjectStatus projectStatus, Issue issue) =>
        projectStatus == ProjectStatus.Active &&
        (member.Role is ProjectRole.Owner or ProjectRole.Manager ||
         member.Role == ProjectRole.Member && issue.Status == IssueStatus.Draft && issue.CreatorUserId == member.UserId);

    public static bool CanAssign(ProjectMembership member, ProjectStatus projectStatus) =>
        projectStatus == ProjectStatus.Active && member.Role is ProjectRole.Owner or ProjectRole.Manager;

    public static bool CanComment(ProjectMembership member, ProjectStatus projectStatus) =>
        projectStatus == ProjectStatus.Active && member.Role is not ProjectRole.Viewer;

    public static IReadOnlyCollection<IssueStatus> AllowedTransitions(
        ProjectMembership member, ProjectStatus projectStatus, Issue issue)
    {
        if (projectStatus == ProjectStatus.Archived) return [];
        var manager = member.Role is ProjectRole.Owner or ProjectRole.Manager;
        var assignee = issue.AssigneeUserId == member.UserId;

        return issue.Status switch
        {
            IssueStatus.Draft when manager => [IssueStatus.Approved],
            IssueStatus.Approved when manager || assignee => [IssueStatus.InProgress],
            IssueStatus.InProgress when manager || assignee => [IssueStatus.QA],
            IssueStatus.QA when manager => [IssueStatus.Done, IssueStatus.InProgress],
            IssueStatus.QA when assignee => [IssueStatus.InProgress],
            IssueStatus.Done when manager => [IssueStatus.InProgress],
            _ => []
        };
    }
}
