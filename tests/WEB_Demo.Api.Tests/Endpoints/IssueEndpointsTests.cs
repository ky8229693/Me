using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WEB_Demo.Api.Authorization;
using WEB_Demo.Api.Data;
using WEB_Demo.Api.Models;

namespace WEB_Demo.Api.Tests.Endpoints;

public sealed class IssueEndpointsTests
{
    [Fact]
    public async Task IssueLifecycle_SupportsFilteringWorkflowHistoryAndComments()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var owner = await CreateAdminAsync(factory, client);
        await LoginAsAsync(client, owner.Email);
        var projectId = await CreateProjectAsync(client, "Issue lifecycle");

        var response = await client.PostAsJsonAsync($"/api/projects/{projectId}/issues",
            new CreateIssueRequest("Login bug", "Cannot sign in", "Bug", "Critical", null, null));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await response.Content.ReadFromJsonAsync<ApiResponse<IssueInfo>>())!.Data!;
        created.Status.Should().Be(nameof(IssueStatus.Draft));

        var list = await client.GetFromJsonAsync<ApiResponse<PagedResult<IssueInfo>>>(
            $"/api/projects/{projectId}/issues?search=login&type=Bug&priority=Critical&page=1&pageSize=20");
        list!.Data!.Items.Should().ContainSingle(item => item.Id == created.Id);

        var updatedResponse = await client.PutAsJsonAsync($"/api/projects/{projectId}/issues/{created.Id}",
            new UpdateIssueRequest("Login bug updated", "Cannot sign in", "Bug", "Critical", null));
        updatedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await updatedResponse.Content.ReadFromJsonAsync<ApiResponse<IssueInfo>>())!.Data!.Title
            .Should().Be("Login bug updated");

        (await client.PostAsJsonAsync($"/api/projects/{projectId}/issues/{created.Id}/transitions",
            new TransitionIssueRequest("Approved"))).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsJsonAsync($"/api/projects/{projectId}/issues/{created.Id}/comments",
            new CreateIssueCommentRequest("Reproduced."))).StatusCode.Should().Be(HttpStatusCode.OK);

        var history = await client.GetFromJsonAsync<ApiResponse<IReadOnlyCollection<IssueHistoryInfo>>>(
            $"/api/projects/{projectId}/issues/{created.Id}/history");
        history!.Data.Should().Contain(item => item.ChangeType == nameof(IssueHistoryChangeType.Created));
        history.Data.Should().Contain(item => item.ChangeType == nameof(IssueHistoryChangeType.StatusChanged));
        var historyCount = history.Data!.Count;
        (await client.PostAsJsonAsync($"/api/projects/{projectId}/issues/{created.Id}/transitions",
            new TransitionIssueRequest("Done"))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var historyAfterFailure = await client.GetFromJsonAsync<ApiResponse<IReadOnlyCollection<IssueHistoryInfo>>>(
            $"/api/projects/{projectId}/issues/{created.Id}/history");
        historyAfterFailure!.Data.Should().HaveCount(historyCount);
        var comments = await client.GetFromJsonAsync<ApiResponse<IReadOnlyCollection<IssueCommentInfo>>>(
            $"/api/projects/{projectId}/issues/{created.Id}/comments");
        comments!.Data.Should().ContainSingle(item => item.Content == "Reproduced.");
    }

    [Fact]
    public async Task ProjectBoundaryAndArchive_BlockUnauthorizedMutations()
    {
        await using var factory = CreateFactory();
        var ownerClient = factory.CreateClient();
        var owner = await CreateAdminAsync(factory, ownerClient);
        await LoginAsAsync(ownerClient, owner.Email);
        var projectId = await CreateProjectAsync(ownerClient, "Private Issues");
        var created = (await (await ownerClient.PostAsJsonAsync($"/api/projects/{projectId}/issues",
            new CreateIssueRequest("Secret", null, "Feature", "Low", null, null)))
            .Content.ReadFromJsonAsync<ApiResponse<IssueInfo>>())!.Data!;

        var otherClient = factory.CreateClient();
        var other = await CreateAdminAsync(factory, otherClient);
        await LoginAsAsync(otherClient, other.Email);
        (await otherClient.GetAsync($"/api/projects/{projectId}/issues/{created.Id}"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);

        (await ownerClient.PostAsync($"/api/projects/{projectId}/archive", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await ownerClient.PostAsJsonAsync($"/api/projects/{projectId}/issues",
            new CreateIssueRequest("Blocked", null, "Bug", "High", null, null)))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ownerClient.PostAsJsonAsync($"/api/projects/{projectId}/issues/{created.Id}/comments",
            new CreateIssueCommentRequest("Blocked"))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RemovingAssignedMember_ClearsAssignmentAndRecordsHistory()
    {
        await using var factory = CreateFactory();
        var ownerClient = factory.CreateClient();
        var owner = await CreateAdminAsync(factory, ownerClient);
        await LoginAsAsync(ownerClient, owner.Email);
        var projectId = await CreateProjectAsync(ownerClient, "Assignment cleanup");
        var memberClient = factory.CreateClient();
        var member = await RegisterAsync(memberClient);
        (await ownerClient.PostAsJsonAsync($"/api/projects/{projectId}/members",
            new AddProjectMemberRequest(member.User.Email, nameof(ProjectRole.Member)))).StatusCode.Should().Be(HttpStatusCode.OK);
        var created = (await (await ownerClient.PostAsJsonAsync($"/api/projects/{projectId}/issues",
            new CreateIssueRequest("Assigned", null, "Bug", "High", member.User.Id, null)))
            .Content.ReadFromJsonAsync<ApiResponse<IssueInfo>>())!.Data!;

        (await ownerClient.DeleteAsync($"/api/projects/{projectId}/members/{member.User.Id}"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        var issue = await ownerClient.GetFromJsonAsync<ApiResponse<IssueInfo>>(
            $"/api/projects/{projectId}/issues/{created.Id}");
        issue!.Data!.AssigneeUserId.Should().BeNull();
        var history = await ownerClient.GetFromJsonAsync<ApiResponse<IReadOnlyCollection<IssueHistoryInfo>>>(
            $"/api/projects/{projectId}/issues/{created.Id}/history");
        history!.Data.Should().Contain(item => item.ChangeType == nameof(IssueHistoryChangeType.AssignmentChanged) && item.PreviousValue == member.User.Id);
    }

    [Fact]
    public async Task LabelsBoardAndDashboard_RespectProjectScopeAndWorkflow()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var owner = await CreateAdminAsync(factory, client);
        await LoginAsAsync(client, owner.Email);
        var projectId = await CreateProjectAsync(client, "Kanban workspace");

        var labelResponse = await client.PostAsJsonAsync($"/api/projects/{projectId}/labels",
            new CreateProjectLabelRequest("Backend", "#2563EB"));
        labelResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var label = (await labelResponse.Content.ReadFromJsonAsync<ApiResponse<IssueLabelInfo>>())!.Data!;
        (await client.PostAsJsonAsync($"/api/projects/{projectId}/labels",
            new CreateProjectLabelRequest("backend", "#22C55E"))).StatusCode.Should().Be(HttpStatusCode.Conflict);

        var issueResponse = await client.PostAsJsonAsync($"/api/projects/{projectId}/issues",
            new CreateIssueRequest("Build board", null, "Feature", "High", owner.Id, DateTimeOffset.UtcNow.AddDays(-1), [label.Id]));
        issueResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var issue = (await issueResponse.Content.ReadFromJsonAsync<ApiResponse<IssueInfo>>())!.Data!;
        issue.Labels.Should().ContainSingle(item => item.Id == label.Id);

        var filtered = await client.GetFromJsonAsync<ApiResponse<PagedResult<IssueInfo>>>(
            $"/api/projects/{projectId}/issues?labels={label.Id}");
        filtered!.Data!.Items.Should().ContainSingle(item => item.Id == issue.Id);
        var board = await client.GetFromJsonAsync<ApiResponse<IssueBoardInfo>>(
            $"/api/projects/{projectId}/issues/board?labels={label.Id}");
        board!.Data!.Statuses.Should().Equal("Draft", "Approved", "InProgress", "QA", "Done");
        board.Data.Items.Should().ContainSingle(item => item.Id == issue.Id);

        var dashboard = await client.GetFromJsonAsync<ApiResponse<DashboardInfo>>("/api/dashboard");
        dashboard!.Data!.AssignedCount.Should().Be(1);
        dashboard.Data.OverdueCount.Should().Be(1);
        dashboard.Data.AssignedIssues.Should().ContainSingle(item => item.Id == issue.Id);

        (await client.PostAsJsonAsync($"/api/projects/{projectId}/issues/{issue.Id}/transitions",
            new TransitionIssueRequest("Approved"))).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.DeleteAsync($"/api/projects/{projectId}/labels/{label.Id}"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        var history = await client.GetFromJsonAsync<ApiResponse<IReadOnlyCollection<IssueHistoryInfo>>>(
            $"/api/projects/{projectId}/issues/{issue.Id}/history");
        history!.Data.Should().Contain(item => item.FieldName == "Labels");
    }

    [Fact]
    public async Task ViewerCannotManageLabelsAndNonMemberCannotReadBoardOrDashboardActivity()
    {
        await using var factory = CreateFactory();
        var ownerClient = factory.CreateClient();
        var owner = await CreateAdminAsync(factory, ownerClient);
        await LoginAsAsync(ownerClient, owner.Email);
        var projectId = await CreateProjectAsync(ownerClient, "Label authorization");
        var viewerClient = factory.CreateClient();
        var viewer = await RegisterAsync(viewerClient);
        (await ownerClient.PostAsJsonAsync($"/api/projects/{projectId}/members",
            new AddProjectMemberRequest(viewer.User.Email, nameof(ProjectRole.Viewer)))).StatusCode.Should().Be(HttpStatusCode.OK);
        await LoginAsAsync(viewerClient, viewer.User.Email);

        (await viewerClient.PostAsJsonAsync($"/api/projects/{projectId}/labels",
            new CreateProjectLabelRequest("Forbidden", "#DC2626"))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await viewerClient.GetAsync($"/api/projects/{projectId}/issues/board")).StatusCode.Should().Be(HttpStatusCode.OK);

        var outsiderClient = factory.CreateClient();
        var outsider = await RegisterAsync(outsiderClient);
        await LoginAsAsync(outsiderClient, outsider.User.Email);
        (await outsiderClient.GetAsync($"/api/projects/{projectId}/issues/board")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        var dashboard = await outsiderClient.GetFromJsonAsync<ApiResponse<DashboardInfo>>("/api/dashboard");
        dashboard!.Data!.AssignedCount.Should().Be(0);
        dashboard.Data.RecentActivity.Should().BeEmpty();
    }

    private static async Task<Guid> CreateProjectAsync(HttpClient client, string name)
    {
        var body = await (await client.PostAsJsonAsync("/api/projects", new CreateProjectRequest(name, null)))
            .Content.ReadFromJsonAsync<ApiResponse<ProjectInfo>>();
        return body!.Data!.Id;
    }

    private static async Task<AdminUser> CreateAdminAsync(WebApplicationFactory<Program> factory, HttpClient client)
    {
        var registered = await RegisterAsync(client);
        await using var scope = factory.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var identityUser = await manager.FindByIdAsync(registered.User.Id);
        (await manager.AddToRoleAsync(identityUser!, AppRoles.Admin)).Succeeded.Should().BeTrue();
        (await manager.UpdateSecurityStampAsync(identityUser!)).Succeeded.Should().BeTrue();
        return registered.User;
    }

    private static async Task<(AdminUser User, string Token)> RegisterAsync(HttpClient client)
    {
        var email = $"issue-{Guid.NewGuid():N}@example.com";
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "123456"));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        return (new AdminUser(body!.Data!.User.Id, email, true, body.Data.User.Roles), body.Data.AccessToken);
    }

    private static async Task LoginAsAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "123456"));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Data!.AccessToken);
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        var databaseName = $"issue-tests-{Guid.NewGuid():N}";
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
            });
        });
    }
}
