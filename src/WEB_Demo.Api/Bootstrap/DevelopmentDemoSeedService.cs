using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WEB_Demo.Api.Data;

namespace WEB_Demo.Api.Bootstrap;

public sealed class DevelopmentDemoSeedService(AppDbContext dbContext, UserManager<IdentityUser> userManager)
{
    public async Task<DemoSeedResult> SeedAsync(string ownerEmail, bool allowDevelopmentSeed)
    {
        if (!allowDevelopmentSeed)
            return DemoSeedResult.Failure("Demo data can only be seeded in the Development environment.");
        var owner = await userManager.FindByEmailAsync(ownerEmail.Trim());
        if (owner is null)
            return DemoSeedResult.Failure("The demo Project owner account was not found.");
        if (owner.LockoutEnd is { } lockoutEnd && lockoutEnd > DateTimeOffset.UtcNow)
            return DemoSeedResult.Failure("The demo Project owner account is disabled.");

        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync()
            : null;
        try
        {
            var now = DateTimeOffset.UtcNow;
            var project = await dbContext.Projects.SingleOrDefaultAsync(item => item.Id == DemoSeedCatalog.ProjectId);
            if (project is not null && project.Name != DemoSeedCatalog.ProjectName)
                return DemoSeedResult.Failure("The reserved demo Project identifier is already in use.");
            if (project is null)
            {
                project = new Project { Id = DemoSeedCatalog.ProjectId, Name = DemoSeedCatalog.ProjectName,
                    Description = "Dogfooding workspace for planning and demonstrating WEB_Demo development.",
                    Status = ProjectStatus.Active, CreatedAt = now.AddDays(-45), UpdatedAt = now };
                dbContext.Projects.Add(project);
            }

            var membership = await dbContext.ProjectMemberships.SingleOrDefaultAsync(item =>
                item.ProjectId == DemoSeedCatalog.ProjectId && item.UserId == owner.Id);
            if (membership is null)
            {
                dbContext.ProjectMemberships.Add(new ProjectMembership { ProjectId = DemoSeedCatalog.ProjectId,
                    UserId = owner.Id, Role = ProjectRole.Owner, CreatedAt = now, UpdatedAt = now });
            }
            else if (membership.Role != ProjectRole.Owner)
            {
                membership.Role = ProjectRole.Owner;
                membership.UpdatedAt = now;
            }

            var existingLabels = await dbContext.ProjectLabels
                .Where(item => DemoSeedCatalog.Labels.Select(seed => seed.Id).Contains(item.Id)).ToDictionaryAsync(item => item.Id);
            foreach (var seed in DemoSeedCatalog.Labels)
            {
                if (existingLabels.TryGetValue(seed.Id, out var existing))
                {
                    if (existing.ProjectId != DemoSeedCatalog.ProjectId)
                        return DemoSeedResult.Failure($"The reserved demo label identifier for {seed.Name} is already in use.");
                    continue;
                }
                dbContext.ProjectLabels.Add(new ProjectLabel { Id = seed.Id, ProjectId = DemoSeedCatalog.ProjectId,
                    Name = seed.Name, NormalizedName = seed.Name.ToUpperInvariant(), Color = seed.Color,
                    CreatedAt = now, UpdatedAt = now });
            }

            var existingIssues = await dbContext.Issues
                .Where(item => DemoSeedCatalog.Issues.Select(seed => seed.Id).Contains(item.Id)).ToDictionaryAsync(item => item.Id);
            foreach (var seed in DemoSeedCatalog.Issues)
            {
                if (existingIssues.TryGetValue(seed.Id, out var existing) && existing.ProjectId != DemoSeedCatalog.ProjectId)
                    return DemoSeedResult.Failure($"The reserved demo Issue identifier for {seed.Title} is already in use.");
                if (!existingIssues.ContainsKey(seed.Id))
                {
                    dbContext.Issues.Add(new Issue { Id = seed.Id, ProjectId = DemoSeedCatalog.ProjectId,
                        Title = seed.Title, Description = seed.Description, Type = seed.Type, Priority = seed.Priority,
                        Status = seed.Status, CreatorUserId = owner.Id, AssigneeUserId = seed.Status == IssueStatus.Done ? null : owner.Id,
                        DueDate = now.AddDays(seed.DueDays), CreatedAt = now.AddDays(seed.CreatedDays), UpdatedAt = now.AddDays(Math.Min(0, seed.CreatedDays / 2)) });
                }
            }

            await dbContext.SaveChangesAsync();
            var labelIdsByName = DemoSeedCatalog.Labels.ToDictionary(item => item.Name, item => item.Id);
            var existingMappings = await dbContext.IssueLabels.Where(item =>
                DemoSeedCatalog.Issues.Select(seed => seed.Id).Contains(item.IssueId)).ToArrayAsync();
            foreach (var issue in DemoSeedCatalog.Issues)
            foreach (var labelName in issue.Labels)
            {
                var labelId = labelIdsByName[labelName];
                if (!existingMappings.Any(item => item.IssueId == issue.Id && item.LabelId == labelId))
                    dbContext.IssueLabels.Add(new IssueLabel { IssueId = issue.Id, LabelId = labelId });
            }

            var activityIssue = DemoSeedCatalog.Issues.Single(item => item.Status == IssueStatus.InProgress);
            if (!await dbContext.IssueComments.AnyAsync(item => item.Id == DemoSeedCatalog.CommentId))
                dbContext.IssueComments.Add(new IssueComment { Id = DemoSeedCatalog.CommentId,
                    ProjectId = DemoSeedCatalog.ProjectId, IssueId = activityIssue.Id, AuthorUserId = owner.Id,
                    Content = "Dashboard API and responsive workbench are ready for integrated review.", CreatedAt = now.AddHours(-6) });
            if (!await dbContext.IssueHistory.AnyAsync(item => item.Id == DemoSeedCatalog.HistoryId))
                dbContext.IssueHistory.Add(new IssueHistory { Id = DemoSeedCatalog.HistoryId,
                    ProjectId = DemoSeedCatalog.ProjectId, IssueId = activityIssue.Id, ActorUserId = owner.Id,
                    ChangeType = IssueHistoryChangeType.StatusChanged, FieldName = nameof(Issue.Status),
                    PreviousValue = IssueStatus.Approved.ToString(), NewValue = IssueStatus.InProgress.ToString(), CreatedAt = now.AddHours(-8) });
            if (!await dbContext.ProjectAuditEvents.AnyAsync(item => item.ProjectId == DemoSeedCatalog.ProjectId &&
                item.Action == "demo.seeded" && item.TargetId == DemoSeedCatalog.ProjectId.ToString()))
                dbContext.ProjectAuditEvents.Add(new ProjectAuditEvent { ProjectId = DemoSeedCatalog.ProjectId,
                    ActorUserId = owner.Id, Action = "demo.seeded", TargetType = "Project",
                    TargetId = DemoSeedCatalog.ProjectId.ToString(), NewValue = DemoSeedCatalog.ProjectName, CreatedAt = now });

            await SeedChineseProjectAsync(owner, now);
            await dbContext.SaveChangesAsync();
            if (transaction is not null) await transaction.CommitAsync();
            return DemoSeedResult.Success($"Demo data ready: {DemoSeedCatalog.ProjectName} and {ChineseDemoSeedCatalog.ProjectName} ({DemoSeedCatalog.Issues.Count + ChineseDemoSeedCatalog.Issues.Count} Issues).");
        }
        catch (Exception exception)
        {
            if (transaction is not null) await transaction.RollbackAsync();
            return DemoSeedResult.Failure($"Demo seed failed: {exception.Message}");
        }
    }

    private async Task SeedChineseProjectAsync(IdentityUser owner, DateTimeOffset now)
    {
        var project = await dbContext.Projects.SingleOrDefaultAsync(item => item.Id == ChineseDemoSeedCatalog.ProjectId);
        if (project is not null && project.Name != ChineseDemoSeedCatalog.ProjectName)
            throw new InvalidOperationException("The reserved Chinese demo Project identifier is already in use.");
        if (project is null)
        {
            dbContext.Projects.Add(new Project
            {
                Id = ChineseDemoSeedCatalog.ProjectId,
                Name = ChineseDemoSeedCatalog.ProjectName,
                Description = ChineseDemoSeedCatalog.ProjectDescription,
                Status = ProjectStatus.Active,
                CreatedAt = now.AddDays(-45),
                UpdatedAt = now
            });
        }

        var membership = await dbContext.ProjectMemberships.SingleOrDefaultAsync(item =>
            item.ProjectId == ChineseDemoSeedCatalog.ProjectId && item.UserId == owner.Id);
        if (membership is null)
        {
            dbContext.ProjectMemberships.Add(new ProjectMembership
            {
                ProjectId = ChineseDemoSeedCatalog.ProjectId,
                UserId = owner.Id,
                Role = ProjectRole.Owner,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else if (membership.Role != ProjectRole.Owner)
        {
            membership.Role = ProjectRole.Owner;
            membership.UpdatedAt = now;
        }

        var labelIds = ChineseDemoSeedCatalog.Labels.Select(seed => seed.Id).ToArray();
        var existingLabels = await dbContext.ProjectLabels.Where(item => labelIds.Contains(item.Id)).ToDictionaryAsync(item => item.Id);
        foreach (var seed in ChineseDemoSeedCatalog.Labels)
        {
            if (existingLabels.TryGetValue(seed.Id, out var existing))
            {
                if (existing.ProjectId != ChineseDemoSeedCatalog.ProjectId)
                    throw new InvalidOperationException($"The reserved Chinese demo label identifier for {seed.Name} is already in use.");
                continue;
            }
            dbContext.ProjectLabels.Add(new ProjectLabel
            {
                Id = seed.Id, ProjectId = ChineseDemoSeedCatalog.ProjectId, Name = seed.Name,
                NormalizedName = seed.Name.ToUpperInvariant(), Color = seed.Color, CreatedAt = now, UpdatedAt = now
            });
        }

        var issueIds = ChineseDemoSeedCatalog.Issues.Select(seed => seed.Id).ToArray();
        var existingIssues = await dbContext.Issues.Where(item => issueIds.Contains(item.Id)).ToDictionaryAsync(item => item.Id);
        foreach (var seed in ChineseDemoSeedCatalog.Issues)
        {
            if (existingIssues.TryGetValue(seed.Id, out var existing) && existing.ProjectId != ChineseDemoSeedCatalog.ProjectId)
                throw new InvalidOperationException($"The reserved Chinese demo Issue identifier for {seed.Title} is already in use.");
            if (!existingIssues.ContainsKey(seed.Id))
            {
                dbContext.Issues.Add(new Issue
                {
                    Id = seed.Id, ProjectId = ChineseDemoSeedCatalog.ProjectId, Title = seed.Title,
                    Description = seed.Description, Type = seed.Type, Priority = seed.Priority, Status = seed.Status,
                    CreatorUserId = owner.Id, AssigneeUserId = seed.Status == IssueStatus.Done ? null : owner.Id,
                    DueDate = now.AddDays(seed.DueDays), CreatedAt = now.AddDays(seed.CreatedDays),
                    UpdatedAt = now.AddDays(Math.Min(0, seed.CreatedDays / 2))
                });
            }
        }

        await dbContext.SaveChangesAsync();
        var labelIdsByName = ChineseDemoSeedCatalog.Labels.ToDictionary(item => item.Name, item => item.Id);
        var existingMappings = await dbContext.IssueLabels.Where(item => issueIds.Contains(item.IssueId)).ToArrayAsync();
        foreach (var issue in ChineseDemoSeedCatalog.Issues)
        foreach (var labelName in issue.Labels)
        {
            var labelId = labelIdsByName[labelName];
            if (!existingMappings.Any(item => item.IssueId == issue.Id && item.LabelId == labelId))
                dbContext.IssueLabels.Add(new IssueLabel { IssueId = issue.Id, LabelId = labelId });
        }

        var activityIssue = ChineseDemoSeedCatalog.Issues.Single(item => item.Status == IssueStatus.InProgress);
        if (!await dbContext.IssueComments.AnyAsync(item => item.Id == ChineseDemoSeedCatalog.CommentId))
            dbContext.IssueComments.Add(new IssueComment
            {
                Id = ChineseDemoSeedCatalog.CommentId, ProjectId = ChineseDemoSeedCatalog.ProjectId,
                IssueId = activityIssue.Id, AuthorUserId = owner.Id,
                Content = "Dashboard API 與響應式工作台已完成，等待整合驗收。", CreatedAt = now.AddHours(-6)
            });
        if (!await dbContext.IssueHistory.AnyAsync(item => item.Id == ChineseDemoSeedCatalog.HistoryId))
            dbContext.IssueHistory.Add(new IssueHistory
            {
                Id = ChineseDemoSeedCatalog.HistoryId, ProjectId = ChineseDemoSeedCatalog.ProjectId,
                IssueId = activityIssue.Id, ActorUserId = owner.Id, ChangeType = IssueHistoryChangeType.StatusChanged,
                FieldName = nameof(Issue.Status), PreviousValue = IssueStatus.Approved.ToString(),
                NewValue = IssueStatus.InProgress.ToString(), CreatedAt = now.AddHours(-8)
            });
        if (!await dbContext.ProjectAuditEvents.AnyAsync(item => item.ProjectId == ChineseDemoSeedCatalog.ProjectId &&
            item.Action == "demo.seeded" && item.TargetId == ChineseDemoSeedCatalog.ProjectId.ToString()))
            dbContext.ProjectAuditEvents.Add(new ProjectAuditEvent
            {
                ProjectId = ChineseDemoSeedCatalog.ProjectId, ActorUserId = owner.Id, Action = "demo.seeded",
                TargetType = "Project", TargetId = ChineseDemoSeedCatalog.ProjectId.ToString(),
                NewValue = ChineseDemoSeedCatalog.ProjectName, CreatedAt = now
            });
    }
}
