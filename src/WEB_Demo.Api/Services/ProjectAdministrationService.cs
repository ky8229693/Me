using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WEB_Demo.Api.Authorization;
using WEB_Demo.Api.Data;
using WEB_Demo.Api.Models;

namespace WEB_Demo.Api.Services;

public sealed class ProjectAdministrationService(
    AppDbContext dbContext,
    UserManager<IdentityUser> userManager,
    ProjectAccessService access)
{
    public async Task<AdministrationResult<ProjectInfo>> CreateAsync(
        string actorUserId, string? name, string? description)
    {
        var normalized = ValidateName(name);
        if (normalized is null) return AdministrationResult<ProjectInfo>.Validation("Project name must contain between 2 and 100 characters.");
        var normalizedDescription = NormalizeDescription(description);
        if (normalizedDescription is null && !string.IsNullOrWhiteSpace(description))
            return AdministrationResult<ProjectInfo>.Validation("Project description cannot exceed 2000 characters.");

        var now = DateTimeOffset.UtcNow;
        var project = new Project
        {
            Name = normalized,
            Description = normalizedDescription,
            CreatedAt = now,
            UpdatedAt = now
        };
        var membership = new ProjectMembership
        {
            ProjectId = project.Id,
            UserId = actorUserId,
            Role = ProjectRole.Owner,
            CreatedAt = now,
            UpdatedAt = now
        };

        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync()
            : null;
        dbContext.Projects.Add(project);
        dbContext.ProjectMemberships.Add(membership);
        AddAudit(project.Id, actorUserId, "project.created", "Project", project.Id.ToString(), null, project.Name, now);
        await dbContext.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        return AdministrationResult<ProjectInfo>.Success(ToInfo(project, ProjectRole.Owner), "Project created.");
    }

    public async Task<IReadOnlyCollection<ProjectInfo>> ListAsync(string userId) =>
        await dbContext.ProjectMemberships.AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderBy(item => item.Project.Name)
            .Select(item => new ProjectInfo(
                item.Project.Id, item.Project.Name, item.Project.Description,
                item.Project.Status.ToString(), item.Role.ToString(),
                item.Project.CreatedAt, item.Project.UpdatedAt))
            .ToArrayAsync();

    public async Task<AdministrationResult<ProjectInfo>> GetAsync(Guid projectId, string userId)
    {
        var membership = await access.GetMembershipAsync(projectId, userId);
        if (membership is null) return AdministrationResult<ProjectInfo>.NotFound("Project was not found.");
        var project = await dbContext.Projects.AsNoTracking().SingleOrDefaultAsync(item => item.Id == projectId);
        return project is null
            ? AdministrationResult<ProjectInfo>.NotFound("Project was not found.")
            : AdministrationResult<ProjectInfo>.Success(ToInfo(project, membership.Role));
    }

    public async Task<AdministrationResult<ProjectInfo>> UpdateAsync(
        Guid projectId, string actorUserId, string? name, string? description)
    {
        var project = await dbContext.Projects.SingleOrDefaultAsync(item => item.Id == projectId);
        if (project is null) return AdministrationResult<ProjectInfo>.NotFound("Project was not found.");
        var membership = await access.GetMembershipAsync(projectId, actorUserId);
        if (membership is null) return AdministrationResult<ProjectInfo>.NotFound("Project was not found.");
        if (!ProjectAccessService.IsAllowed(membership, project.Status, ProjectAction.Update))
            return AdministrationResult<ProjectInfo>.Forbidden("You cannot update this project.");

        var normalized = ValidateName(name);
        if (normalized is null) return AdministrationResult<ProjectInfo>.Validation("Project name must contain between 2 and 100 characters.");
        var normalizedDescription = NormalizeDescription(description);
        if (normalizedDescription is null && !string.IsNullOrWhiteSpace(description))
            return AdministrationResult<ProjectInfo>.Validation("Project description cannot exceed 2000 characters.");

        var now = DateTimeOffset.UtcNow;
        var previous = project.Name;
        project.Name = normalized;
        project.Description = normalizedDescription;
        project.UpdatedAt = now;
        AddAudit(projectId, actorUserId, "project.updated", "Project", projectId.ToString(), previous, project.Name, now);
        await dbContext.SaveChangesAsync();
        return AdministrationResult<ProjectInfo>.Success(ToInfo(project, membership.Role), "Project updated.");
    }

    public Task<AdministrationResult<ProjectInfo>> ArchiveAsync(Guid projectId, string actorUserId) =>
        ChangeStatusAsync(projectId, actorUserId, ProjectStatus.Archived);

    public Task<AdministrationResult<ProjectInfo>> RestoreAsync(Guid projectId, string actorUserId) =>
        ChangeStatusAsync(projectId, actorUserId, ProjectStatus.Active);

    public async Task<AdministrationResult<IReadOnlyCollection<ProjectMemberInfo>>> GetMembersAsync(
        Guid projectId, string userId)
    {
        if (await access.GetMembershipAsync(projectId, userId) is null)
            return AdministrationResult<IReadOnlyCollection<ProjectMemberInfo>>.NotFound("Project was not found.");
        var members = await dbContext.ProjectMemberships.AsNoTracking()
            .Where(item => item.ProjectId == projectId)
            .OrderBy(item => item.User.Email)
            .Select(item => new ProjectMemberInfo(
                item.UserId, item.User.Email ?? string.Empty, item.Role.ToString(), item.CreatedAt, item.UpdatedAt))
            .ToArrayAsync();
        return AdministrationResult<IReadOnlyCollection<ProjectMemberInfo>>.Success(members);
    }

    public async Task<AdministrationResult<IReadOnlyCollection<ProjectMemberCandidate>>> SearchMemberCandidatesAsync(
        Guid projectId, string actorUserId, string? search)
    {
        var context = await GetMemberMutationContextAsync(projectId, actorUserId);
        if (context.Failure is not null)
        {
            return context.Failure.Status switch
            {
                AdministrationResultStatus.NotFound => AdministrationResult<IReadOnlyCollection<ProjectMemberCandidate>>.NotFound(context.Failure.Message),
                AdministrationResultStatus.Forbidden => AdministrationResult<IReadOnlyCollection<ProjectMemberCandidate>>.Forbidden(context.Failure.Message),
                _ => AdministrationResult<IReadOnlyCollection<ProjectMemberCandidate>>.Conflict(context.Failure.Message)
            };
        }

        var normalized = search?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalized.Length is < 2 or > 100)
            return AdministrationResult<IReadOnlyCollection<ProjectMemberCandidate>>.Validation(
                "Search must contain between 2 and 100 characters.");

        var now = DateTimeOffset.UtcNow;
        var candidates = await userManager.Users.AsNoTracking()
            .Where(user => user.NormalizedEmail != null && user.NormalizedEmail.Contains(normalized))
            .Where(user => user.LockoutEnd == null || user.LockoutEnd <= now)
            .Where(user => !dbContext.ProjectMemberships.Any(member =>
                member.ProjectId == projectId && member.UserId == user.Id))
            .OrderBy(user => user.NormalizedEmail)
            .Take(10)
            .Select(user => new ProjectMemberCandidate(user.Id, user.Email ?? string.Empty))
            .ToArrayAsync();
        return AdministrationResult<IReadOnlyCollection<ProjectMemberCandidate>>.Success(candidates);
    }

    public async Task<AdministrationResult<ProjectMemberInfo>> AddMemberAsync(
        Guid projectId, string actorUserId, string? email, string? role)
    {
        var context = await GetMemberMutationContextAsync(projectId, actorUserId);
        if (context.Failure is not null) return ConvertMemberFailure(context.Failure);
        if (string.IsNullOrWhiteSpace(email)) return AdministrationResult<ProjectMemberInfo>.Validation("User email is required.");
        if (!TryParseRole(role, out var desired)) return AdministrationResult<ProjectMemberInfo>.Validation("Project role is invalid.");
        if (!CanManageRole(context.Actor!.Role, desired)) return AdministrationResult<ProjectMemberInfo>.Forbidden("You cannot assign that project role.");
        if (await userManager.FindByEmailAsync(email.Trim()) is not { } user) return AdministrationResult<ProjectMemberInfo>.NotFound("User was not found.");
        var userId = user.Id;
        if (await dbContext.ProjectMemberships.AnyAsync(item => item.ProjectId == projectId && item.UserId == userId))
            return AdministrationResult<ProjectMemberInfo>.Conflict("User is already a project member.");

        var now = DateTimeOffset.UtcNow;
        var member = new ProjectMembership { ProjectId = projectId, UserId = userId, Role = desired, CreatedAt = now, UpdatedAt = now };
        dbContext.ProjectMemberships.Add(member);
        AddAudit(projectId, actorUserId, "membership.added", "User", userId, null, desired.ToString(), now);
        await dbContext.SaveChangesAsync();
        return AdministrationResult<ProjectMemberInfo>.Success(ToMemberInfo(member, user), "Project member added.");
    }

    public async Task<AdministrationResult<ProjectMemberInfo>> ChangeMemberRoleAsync(
        Guid projectId, string actorUserId, string targetUserId, string? role)
    {
        var context = await GetMemberMutationContextAsync(projectId, actorUserId);
        if (context.Failure is not null) return ConvertMemberFailure(context.Failure);
        if (!TryParseRole(role, out var desired)) return AdministrationResult<ProjectMemberInfo>.Validation("Project role is invalid.");
        var target = await dbContext.ProjectMemberships.Include(item => item.User)
            .SingleOrDefaultAsync(item => item.ProjectId == projectId && item.UserId == targetUserId);
        if (target is null) return AdministrationResult<ProjectMemberInfo>.NotFound("Project member was not found.");
        if (!CanManageExistingRole(context.Actor!.Role, target.Role, desired))
            return AdministrationResult<ProjectMemberInfo>.Forbidden("You cannot change that project role.");
        if (target.Role == ProjectRole.Owner && desired != ProjectRole.Owner &&
            await dbContext.ProjectMemberships.CountAsync(item => item.ProjectId == projectId && item.Role == ProjectRole.Owner) == 1)
            return AdministrationResult<ProjectMemberInfo>.Conflict("A project must retain at least one Owner.");

        var now = DateTimeOffset.UtcNow;
        var previous = target.Role;
        target.Role = desired;
        target.UpdatedAt = now;
        AddAudit(projectId, actorUserId, "membership.role-changed", "User", targetUserId, previous.ToString(), desired.ToString(), now);
        await dbContext.SaveChangesAsync();
        return AdministrationResult<ProjectMemberInfo>.Success(ToMemberInfo(target, target.User), "Project member role updated.");
    }

    public async Task<AdministrationResult<ProjectMemberInfo>> RemoveMemberAsync(
        Guid projectId, string actorUserId, string targetUserId)
    {
        var context = await GetMemberMutationContextAsync(projectId, actorUserId);
        if (context.Failure is not null) return ConvertMemberFailure(context.Failure);
        var target = await dbContext.ProjectMemberships.Include(item => item.User)
            .SingleOrDefaultAsync(item => item.ProjectId == projectId && item.UserId == targetUserId);
        if (target is null) return AdministrationResult<ProjectMemberInfo>.NotFound("Project member was not found.");
        if (!CanManageExistingRole(context.Actor!.Role, target.Role, target.Role))
            return AdministrationResult<ProjectMemberInfo>.Forbidden("You cannot remove that project member.");
        if (target.Role == ProjectRole.Owner &&
            await dbContext.ProjectMemberships.CountAsync(item => item.ProjectId == projectId && item.Role == ProjectRole.Owner) == 1)
            return AdministrationResult<ProjectMemberInfo>.Conflict("A project must retain at least one Owner.");

        var result = ToMemberInfo(target, target.User);
        var now = DateTimeOffset.UtcNow;
        var assignedIssues = await dbContext.Issues
            .Where(item => item.ProjectId == projectId && item.AssigneeUserId == targetUserId)
            .ToArrayAsync();
        foreach (var issue in assignedIssues)
        {
            issue.AssigneeUserId = null;
            issue.UpdatedAt = now;
            dbContext.IssueHistory.Add(new IssueHistory
            {
                ProjectId = projectId,
                IssueId = issue.Id,
                ActorUserId = actorUserId,
                ChangeType = IssueHistoryChangeType.AssignmentChanged,
                FieldName = nameof(Issue.AssigneeUserId),
                PreviousValue = targetUserId,
                NewValue = null,
                CreatedAt = now
            });
        }
        dbContext.ProjectMemberships.Remove(target);
        AddAudit(projectId, actorUserId, "membership.removed", "User", targetUserId, target.Role.ToString(), null, now);
        await dbContext.SaveChangesAsync();
        return AdministrationResult<ProjectMemberInfo>.Success(result, "Project member removed.");
    }

    private async Task<AdministrationResult<ProjectInfo>> ChangeStatusAsync(
        Guid projectId, string actorUserId, ProjectStatus desired)
    {
        var project = await dbContext.Projects.SingleOrDefaultAsync(item => item.Id == projectId);
        if (project is null) return AdministrationResult<ProjectInfo>.NotFound("Project was not found.");
        var membership = await access.GetMembershipAsync(projectId, actorUserId);
        if (membership is null) return AdministrationResult<ProjectInfo>.NotFound("Project was not found.");
        var action = desired == ProjectStatus.Archived ? ProjectAction.Archive : ProjectAction.Restore;
        if (!ProjectAccessService.IsAllowed(membership, project.Status, action))
            return AdministrationResult<ProjectInfo>.Forbidden("Only an Owner can change the project archive state.");

        var previous = project.Status;
        var now = DateTimeOffset.UtcNow;
        project.Status = desired;
        project.UpdatedAt = now;
        AddAudit(projectId, actorUserId, desired == ProjectStatus.Archived ? "project.archived" : "project.restored",
            "Project", projectId.ToString(), previous.ToString(), desired.ToString(), now);
        await dbContext.SaveChangesAsync();
        return AdministrationResult<ProjectInfo>.Success(ToInfo(project, membership.Role),
            desired == ProjectStatus.Archived ? "Project archived." : "Project restored.");
    }

    private async Task<(ProjectMembership? Actor, AdministrationResult<object>? Failure)> GetMemberMutationContextAsync(
        Guid projectId, string actorUserId)
    {
        var project = await dbContext.Projects.AsNoTracking().SingleOrDefaultAsync(item => item.Id == projectId);
        if (project is null) return (null, AdministrationResult<object>.NotFound("Project was not found."));
        var actor = await access.GetMembershipAsync(projectId, actorUserId);
        if (actor is null) return (null, AdministrationResult<object>.NotFound("Project was not found."));
        if (!ProjectAccessService.IsAllowed(actor, project.Status, ProjectAction.ManageMembers))
            return (actor, AdministrationResult<object>.Forbidden(
                project.Status == ProjectStatus.Archived ? "Archived projects are read-only." : "You cannot manage project members."));
        return (actor, null);
    }

    private static AdministrationResult<ProjectMemberInfo> ConvertMemberFailure(AdministrationResult<object> failure) =>
        failure.Status switch
        {
            AdministrationResultStatus.NotFound => AdministrationResult<ProjectMemberInfo>.NotFound(failure.Message),
            AdministrationResultStatus.Forbidden => AdministrationResult<ProjectMemberInfo>.Forbidden(failure.Message),
            _ => AdministrationResult<ProjectMemberInfo>.Conflict(failure.Message)
        };

    private void AddAudit(Guid projectId, string actorUserId, string action, string targetType,
        string targetId, string? previous, string? next, DateTimeOffset now) =>
        dbContext.ProjectAuditEvents.Add(new ProjectAuditEvent
        {
            ProjectId = projectId, ActorUserId = actorUserId, Action = action,
            TargetType = targetType, TargetId = targetId,
            PreviousValue = previous, NewValue = next, CreatedAt = now
        });

    private static string? ValidateName(string? value)
    {
        var normalized = value?.Trim();
        return normalized is { Length: >= 2 and <= 100 } ? normalized : null;
    }

    private static string? NormalizeDescription(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        return normalized is null or { Length: <= 2000 } ? normalized : null;
    }

    private static bool TryParseRole(string? value, out ProjectRole role) =>
        Enum.TryParse(value?.Trim(), true, out role) && Enum.IsDefined(role);

    private static bool CanManageRole(ProjectRole actor, ProjectRole desired) =>
        actor == ProjectRole.Owner || actor == ProjectRole.Manager && desired is ProjectRole.Member or ProjectRole.Viewer;

    private static bool CanManageExistingRole(ProjectRole actor, ProjectRole current, ProjectRole desired) =>
        actor == ProjectRole.Owner ||
        actor == ProjectRole.Manager && current is ProjectRole.Member or ProjectRole.Viewer && desired is ProjectRole.Member or ProjectRole.Viewer;

    private static ProjectInfo ToInfo(Project project, ProjectRole role) =>
        new(project.Id, project.Name, project.Description, project.Status.ToString(), role.ToString(), project.CreatedAt, project.UpdatedAt);

    private static ProjectMemberInfo ToMemberInfo(ProjectMembership membership, IdentityUser user) =>
        new(membership.UserId, user.Email ?? string.Empty, membership.Role.ToString(), membership.CreatedAt, membership.UpdatedAt);
}
