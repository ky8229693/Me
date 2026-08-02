using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using WEB_Demo.Api.Authorization;
using WEB_Demo.Api.Data;
using WEB_Demo.Api.Models;

namespace WEB_Demo.Api.Services;

public sealed partial class ProjectLabelAdministrationService(
    AppDbContext dbContext, ProjectAccessService projectAccess)
{
    public async Task<AdministrationResult<IReadOnlyCollection<IssueLabelInfo>>> ListAsync(
        Guid projectId, string userId)
    {
        if (await projectAccess.GetMembershipAsync(projectId, userId) is null)
            return AdministrationResult<IReadOnlyCollection<IssueLabelInfo>>.NotFound("Project was not found.");
        var items = await dbContext.ProjectLabels.AsNoTracking().Where(item => item.ProjectId == projectId)
            .OrderBy(item => item.Name)
            .Select(item => new IssueLabelInfo(item.Id, item.Name, item.Color)).ToArrayAsync();
        return AdministrationResult<IReadOnlyCollection<IssueLabelInfo>>.Success(items);
    }

    public async Task<AdministrationResult<IssueLabelInfo>> CreateAsync(
        Guid projectId, string userId, CreateProjectLabelRequest request)
    {
        var context = await GetMutationContextAsync(projectId, userId);
        if (context.Failure is not null) return Convert(context.Failure);
        var validation = Validate(request.Name, request.Color, out var name, out var normalized, out var color);
        if (validation is not null) return AdministrationResult<IssueLabelInfo>.Validation(validation);
        if (await dbContext.ProjectLabels.AnyAsync(item => item.ProjectId == projectId && item.NormalizedName == normalized))
            return AdministrationResult<IssueLabelInfo>.Conflict("A label with that name already exists.");
        var now = DateTimeOffset.UtcNow;
        var label = new ProjectLabel { ProjectId = projectId, Name = name!, NormalizedName = normalized!,
            Color = color!, CreatedAt = now, UpdatedAt = now };
        dbContext.ProjectLabels.Add(label);
        await dbContext.SaveChangesAsync();
        return AdministrationResult<IssueLabelInfo>.Success(new(label.Id, label.Name, label.Color), "Label created.");
    }

    public async Task<AdministrationResult<IssueLabelInfo>> UpdateAsync(
        Guid projectId, Guid labelId, string userId, UpdateProjectLabelRequest request)
    {
        var context = await GetMutationContextAsync(projectId, userId);
        if (context.Failure is not null) return Convert(context.Failure);
        var label = await dbContext.ProjectLabels.SingleOrDefaultAsync(item => item.ProjectId == projectId && item.Id == labelId);
        if (label is null) return AdministrationResult<IssueLabelInfo>.NotFound("Label was not found.");
        var validation = Validate(request.Name, request.Color, out var name, out var normalized, out var color);
        if (validation is not null) return AdministrationResult<IssueLabelInfo>.Validation(validation);
        if (await dbContext.ProjectLabels.AnyAsync(item => item.ProjectId == projectId && item.Id != labelId && item.NormalizedName == normalized))
            return AdministrationResult<IssueLabelInfo>.Conflict("A label with that name already exists.");
        label.Name = name!; label.NormalizedName = normalized!; label.Color = color!; label.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();
        return AdministrationResult<IssueLabelInfo>.Success(new(label.Id, label.Name, label.Color), "Label updated.");
    }

    public async Task<AdministrationResult<object>> DeleteAsync(Guid projectId, Guid labelId, string userId)
    {
        var context = await GetMutationContextAsync(projectId, userId);
        if (context.Failure is not null) return context.Failure;
        var label = await dbContext.ProjectLabels.Include(item => item.IssueLabels)
            .SingleOrDefaultAsync(item => item.ProjectId == projectId && item.Id == labelId);
        if (label is null) return AdministrationResult<object>.NotFound("Label was not found.");
        var now = DateTimeOffset.UtcNow;
        foreach (var mapping in label.IssueLabels)
        {
            dbContext.IssueHistory.Add(new IssueHistory { ProjectId = projectId, IssueId = mapping.IssueId,
                ActorUserId = userId, ChangeType = IssueHistoryChangeType.FieldChanged, FieldName = "Labels",
                PreviousValue = label.Name, NewValue = null, CreatedAt = now });
        }
        dbContext.ProjectLabels.Remove(label);
        await dbContext.SaveChangesAsync();
        return AdministrationResult<object>.Success(new { label.Id }, "Label deleted.");
    }

    private async Task<(ProjectMembership? Member, AdministrationResult<object>? Failure)> GetMutationContextAsync(
        Guid projectId, string userId)
    {
        var project = await dbContext.Projects.AsNoTracking().SingleOrDefaultAsync(item => item.Id == projectId);
        var member = await projectAccess.GetMembershipAsync(projectId, userId);
        if (project is null || member is null) return (null, AdministrationResult<object>.NotFound("Project was not found."));
        if (project.Status == ProjectStatus.Archived)
            return (member, AdministrationResult<object>.Forbidden("Archived projects are read-only."));
        if (member.Role is not (ProjectRole.Owner or ProjectRole.Manager))
            return (member, AdministrationResult<object>.Forbidden("You cannot manage project labels."));
        return (member, null);
    }

    private static AdministrationResult<IssueLabelInfo> Convert(AdministrationResult<object> failure) => failure.Status switch
    {
        AdministrationResultStatus.NotFound => AdministrationResult<IssueLabelInfo>.NotFound(failure.Message),
        AdministrationResultStatus.Forbidden => AdministrationResult<IssueLabelInfo>.Forbidden(failure.Message),
        _ => AdministrationResult<IssueLabelInfo>.Conflict(failure.Message)
    };

    private static string? Validate(string? rawName, string? rawColor, out string? name,
        out string? normalized, out string? color)
    {
        name = rawName?.Trim(); normalized = name?.ToUpperInvariant(); color = rawColor?.Trim().ToUpperInvariant();
        if (name is not { Length: >= 1 and <= 40 }) return "Label name must contain between 1 and 40 characters.";
        if (color is null || !ColorPattern().IsMatch(color)) return "Label color must be a hex color such as #2563EB.";
        return null;
    }

    [GeneratedRegex("^#[0-9A-F]{6}$")]
    private static partial Regex ColorPattern();
}
