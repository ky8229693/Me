using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.AspNetCore.Identity;
using WEB_Demo.Api.Authorization;
using WEB_Demo.Api.Data;
using WEB_Demo.Api.Models;

namespace WEB_Demo.Api.Services;

public sealed class AssistantTools(
    string userId, AssistantConversation conversation,
    AssistantConversationService conversations,
    ISpecDocumentRetriever specifications,
    ProjectAdministrationService projects,
    UserManager<IdentityUser> users,
    IPermissionResolver permissions)
{
    public List<string> Sources { get; } = [];
    public AssistantPreparationOutcome? PreparationOutcome { get; private set; }

    [KernelFunction("search_specifications")]
    [Description("Search product specifications and operating procedures. Use for questions about supported behavior or workflow.")]
    public async Task<string> SearchSpecificationsAsync(
        [Description("A focused question or search phrase")] string query,
        CancellationToken cancellationToken = default)
    {
        var result = await specifications.RetrieveAsync(query, cancellationToken);
        Sources.AddRange(result.Chunks.Select(item => item.SourcePath).Distinct().Except(Sources));
        return JsonSerializer.Serialize(result.Chunks.Select(item => new
        {
            item.Capability, item.SourcePath, item.Content
        }));
    }

    [KernelFunction("search_projects")]
    [Description("List projects accessible to the current user. Use this to resolve a project name before issue work.")]
    public async Task<string> SearchProjectsAsync(
        [Description("Optional project name fragment")] string? query = null)
    {
        var available = await projects.ListAsync(userId);
        var normalized = query?.Trim();
        var matches = string.IsNullOrWhiteSpace(normalized)
            ? available
            : available.Where(item => item.Name.Contains(normalized, StringComparison.OrdinalIgnoreCase)).ToArray();
        return JsonSerializer.Serialize(matches.Take(10).Select(item => new { item.Id, item.Name, item.Status }));
    }

    [KernelFunction("prepare_project_creation")]
    [Description("Prepare a project for explicit user confirmation. Never claim that the project was created.")]
    public async Task<string> PrepareProjectAsync(
        [Description("Project name, required, 2 to 100 characters")] string? name,
        [Description("Optional project description")] string? description = null,
        CancellationToken cancellationToken = default)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null || !(await permissions.GetForUserAsync(user)).Contains(
                AppPermissions.ProjectsCreate, StringComparer.Ordinal))
            return Preparation("forbidden", "沒有建立專案的權限。");
        if (string.IsNullOrWhiteSpace(name))
            return Preparation("missing_fields", missingFields: ["name"]);
        var normalized = name.Trim();
        if (normalized.Length is < 2 or > 100)
            return Preparation("validation_error", "專案名稱需為 2 到 100 個字元。");
        if (description?.Length > 2000)
            return Preparation("validation_error", "專案說明不可超過 2000 個字元。");
        var payload = new CreateProjectActionPayload(normalized, string.IsNullOrWhiteSpace(description) ? null : description.Trim());
        var action = await conversations.PrepareAsync(conversation, AssistantPendingActionType.CreateProject,
            payload, payload, cancellationToken);
        return Preparation("confirmation_required", action: action);
    }

    [KernelFunction("prepare_issue_creation")]
    [Description("Prepare an issue for explicit user confirmation. Never claim that the issue was created.")]
    public async Task<string> PrepareIssueAsync(
        [Description("Project identifier; omit only when conversation already has project context")] Guid? projectId,
        [Description("Issue title, required")] string? title,
        [Description("Optional issue description")] string? description = null,
        [Description("Issue type: Feature, Bug, or ChangeRequest")] string type = "Feature",
        [Description("Issue priority: Low, Medium, High, or Critical")] string priority = "Medium",
        [Description("Optional exact assignee user identifier")] string? assigneeUserId = null,
        [Description("Optional due date")] DateTimeOffset? dueDate = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedProjectId = projectId ?? conversation.ContextProjectId;
        var missing = new List<string>();
        if (resolvedProjectId is null) missing.Add("projectId");
        if (string.IsNullOrWhiteSpace(title)) missing.Add("title");
        if (missing.Count > 0)
            return Preparation("missing_fields", missingFields: missing);
        var project = await projects.GetAsync(resolvedProjectId!.Value, userId);
        if (project.Status != AdministrationResultStatus.Success)
            return Preparation("not_found", "找不到可存取的專案。");
        if (!Enum.TryParse<IssueType>(type, true, out _) || !Enum.TryParse<IssuePriority>(priority, true, out _))
            return Preparation("validation_error", "議題類型或優先順序無效。");
        var payload = new CreateIssueActionPayload(resolvedProjectId.Value, title!.Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(), type, priority,
            string.IsNullOrWhiteSpace(assigneeUserId) ? null : assigneeUserId, dueDate);
        var action = await conversations.PrepareAsync(conversation, AssistantPendingActionType.CreateIssue,
            payload, payload, cancellationToken);
        return Preparation("confirmation_required", action: action);
    }

    [KernelFunction("search_assignable_users")]
    [Description("Search members who may be assigned an issue in a project.")]
    public async Task<string> SearchAssignableUsersAsync(
        [Description("Project identifier")] Guid projectId,
        [Description("Email or name fragment")] string query)
    {
        var result = await projects.GetMembersAsync(projectId, userId);
        if (result.Status != AdministrationResultStatus.Success)
            return JsonSerializer.Serialize(new { state = "not_found" });
        var matches = result.Data!.Where(item => item.Email.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(10).Select(item => new { item.UserId, item.Email, item.Role }).ToArray();
        return JsonSerializer.Serialize(new
        {
            state = matches.Length switch { 0 => "not_found", 1 => "resolved", _ => "ambiguous" },
            matches
        });
    }

    private string Preparation(string state, string? message = null,
        IReadOnlyCollection<string>? missingFields = null, AssistantPendingActionInfo? action = null)
    {
        PreparationOutcome = new AssistantPreparationOutcome(state, message, missingFields ?? [], action);
        return JsonSerializer.Serialize(new { state, message, missingFields, action });
    }
}

public sealed record AssistantPreparationOutcome(
    string State, string? Message, IReadOnlyCollection<string> MissingFields,
    AssistantPendingActionInfo? Action);
