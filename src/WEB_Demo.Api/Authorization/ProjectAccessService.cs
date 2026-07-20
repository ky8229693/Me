using Microsoft.EntityFrameworkCore;
using WEB_Demo.Api.Data;

namespace WEB_Demo.Api.Authorization;

public enum ProjectAction
{
    Read,
    Update,
    Archive,
    Restore,
    ManageMembers
}

public sealed class ProjectAccessService(AppDbContext dbContext)
{
    public Task<ProjectMembership?> GetMembershipAsync(Guid projectId, string userId) =>
        dbContext.ProjectMemberships.AsNoTracking()
            .SingleOrDefaultAsync(item => item.ProjectId == projectId && item.UserId == userId);

    public static bool IsAllowed(ProjectMembership membership, ProjectStatus status, ProjectAction action)
    {
        if (action == ProjectAction.Read) return true;
        if (action == ProjectAction.Restore) return membership.Role == ProjectRole.Owner && status == ProjectStatus.Archived;
        if (status == ProjectStatus.Archived) return false;

        return action switch
        {
            ProjectAction.Update => membership.Role is ProjectRole.Owner or ProjectRole.Manager,
            ProjectAction.Archive => membership.Role == ProjectRole.Owner,
            ProjectAction.ManageMembers => membership.Role is ProjectRole.Owner or ProjectRole.Manager,
            _ => false
        };
    }
}

