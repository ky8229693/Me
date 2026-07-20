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

public sealed class ProjectEndpointsTests
{
    [Fact]
    public async Task ProjectLifecycle_EnforcesMembershipArchiveAndAuditRules()
    {
        await using var factory = CreateFactory();
        var ownerClient = factory.CreateClient();
        var owner = await CreateAdminAsync(factory, ownerClient);
        await LoginAsAsync(ownerClient, owner.Email);

        var create = await ownerClient.PostAsJsonAsync("/api/projects", new CreateProjectRequest("PMS", "Foundation"));
        var created = await create.Content.ReadFromJsonAsync<ApiResponse<ProjectInfo>>();
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        created!.Data!.CurrentUserRole.Should().Be(nameof(ProjectRole.Owner));

        var projectId = created.Data.Id;
        var update = await ownerClient.PutAsJsonAsync($"/api/projects/{projectId}", new UpdateProjectRequest("PMS Core", null));
        update.StatusCode.Should().Be(HttpStatusCode.OK);

        (await ownerClient.PostAsync($"/api/projects/{projectId}/archive", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await ownerClient.PutAsJsonAsync($"/api/projects/{projectId}", new UpdateProjectRequest("Blocked", null)))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ownerClient.PostAsync($"/api/projects/{projectId}/restore", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await dbContext.ProjectAuditEvents.CountAsync(item => item.ProjectId == projectId)).Should().Be(4);
    }

    [Fact]
    public async Task AdminWithoutMembership_CannotDiscoverAnotherProject()
    {
        await using var factory = CreateFactory();
        var ownerClient = factory.CreateClient();
        var owner = await CreateAdminAsync(factory, ownerClient);
        await LoginAsAsync(ownerClient, owner.Email);
        var created = await (await ownerClient.PostAsJsonAsync("/api/projects", new CreateProjectRequest("Private Project", null)))
            .Content.ReadFromJsonAsync<ApiResponse<ProjectInfo>>();

        var otherAdminClient = factory.CreateClient();
        var otherAdmin = await CreateAdminAsync(factory, otherAdminClient);
        await LoginAsAsync(otherAdminClient, otherAdmin.Email);

        var list = await (await otherAdminClient.GetAsync("/api/projects"))
            .Content.ReadFromJsonAsync<ApiResponse<IReadOnlyCollection<ProjectInfo>>>();
        list!.Data.Should().BeEmpty();
        (await otherAdminClient.GetAsync($"/api/projects/{created!.Data!.Id}"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MembershipManagement_EnforcesManagerAndLastOwnerRules()
    {
        await using var factory = CreateFactory();
        var ownerClient = factory.CreateClient();
        var owner = await CreateAdminAsync(factory, ownerClient);
        await LoginAsAsync(ownerClient, owner.Email);
        var project = await (await ownerClient.PostAsJsonAsync("/api/projects", new CreateProjectRequest("Membership", null)))
            .Content.ReadFromJsonAsync<ApiResponse<ProjectInfo>>();
        var projectId = project!.Data!.Id;

        var managerClient = factory.CreateClient();
        var manager = await RegisterAsync(managerClient);
        var memberClient = factory.CreateClient();
        var member = await RegisterAsync(memberClient);

        (await ownerClient.PostAsJsonAsync($"/api/projects/{projectId}/members",
            new AddProjectMemberRequest(manager.User.Email, nameof(ProjectRole.Manager)))).StatusCode.Should().Be(HttpStatusCode.OK);
        await LoginAsAsync(managerClient, manager.User.Email);

        (await managerClient.PostAsJsonAsync($"/api/projects/{projectId}/members",
            new AddProjectMemberRequest(member.User.Email, nameof(ProjectRole.Member)))).StatusCode.Should().Be(HttpStatusCode.OK);
        (await managerClient.PutAsJsonAsync($"/api/projects/{projectId}/members/{member.User.Id}/role",
            new UpdateProjectMemberRoleRequest(nameof(ProjectRole.Owner)))).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await ownerClient.PutAsJsonAsync($"/api/projects/{projectId}/members/{owner.Id}/role",
            new UpdateProjectMemberRoleRequest(nameof(ProjectRole.Manager)))).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ownerClient.PostAsJsonAsync($"/api/projects/{projectId}/members",
            new AddProjectMemberRequest(member.User.Email, nameof(ProjectRole.Viewer)))).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateProject_RequiresProjectsCreatePermissionAndValidName()
    {
        await using var factory = CreateFactory();
        var userClient = factory.CreateClient();
        var user = await RegisterAsync(userClient);
        userClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);

        (await userClient.PostAsJsonAsync("/api/projects", new CreateProjectRequest("No Access", null)))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var adminClient = factory.CreateClient();
        var admin = await CreateAdminAsync(factory, adminClient);
        await LoginAsAsync(adminClient, admin.Email);
        (await adminClient.PostAsJsonAsync("/api/projects", new CreateProjectRequest(" ", null)))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MemberCandidates_SearchesByPartialEmailAndRejectsMemberAccess()
    {
        await using var factory = CreateFactory();
        var ownerClient = factory.CreateClient();
        var owner = await CreateAdminAsync(factory, ownerClient);
        await LoginAsAsync(ownerClient, owner.Email);
        var project = await (await ownerClient.PostAsJsonAsync("/api/projects", new CreateProjectRequest("Candidates", null)))
            .Content.ReadFromJsonAsync<ApiResponse<ProjectInfo>>();
        var projectId = project!.Data!.Id;

        var candidateClient = factory.CreateClient();
        var candidate = await RegisterAsync(candidateClient, $"test-candidate-{Guid.NewGuid():N}@example.com");
        var memberClient = factory.CreateClient();
        var member = await RegisterAsync(memberClient, $"member-{Guid.NewGuid():N}@example.com");
        (await ownerClient.PostAsJsonAsync($"/api/projects/{projectId}/members",
            new AddProjectMemberRequest(member.User.Email, nameof(ProjectRole.Member)))).StatusCode.Should().Be(HttpStatusCode.OK);

        var search = await ownerClient.GetFromJsonAsync<ApiResponse<IReadOnlyCollection<ProjectMemberCandidate>>>(
            $"/api/projects/{projectId}/member-candidates?search=test-candidate");
        search!.Data.Should().ContainSingle(item => item.UserId == candidate.User.Id && item.Email == candidate.User.Email);

        await LoginAsAsync(memberClient, member.User.Email);
        (await memberClient.GetAsync($"/api/projects/{projectId}/member-candidates?search=test-candidate"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await ownerClient.PostAsJsonAsync($"/api/projects/{projectId}/members",
            new AddProjectMemberRequest(candidate.User.Email, nameof(ProjectRole.Viewer)))).StatusCode.Should().Be(HttpStatusCode.OK);
        var afterAdd = await ownerClient.GetFromJsonAsync<ApiResponse<IReadOnlyCollection<ProjectMemberCandidate>>>(
            $"/api/projects/{projectId}/member-candidates?search=test-candidate");
        afterAdd!.Data.Should().BeEmpty();
    }

    private static async Task<AdminUser> CreateAdminAsync(WebApplicationFactory<Program> factory, HttpClient client)
    {
        var registered = await RegisterAsync(client, $"admin-{Guid.NewGuid():N}@example.com");
        await using var scope = factory.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var identityUser = await manager.FindByIdAsync(registered.User.Id);
        (await manager.AddToRoleAsync(identityUser!, AppRoles.Admin)).Succeeded.Should().BeTrue();
        (await manager.UpdateSecurityStampAsync(identityUser!)).Succeeded.Should().BeTrue();
        return registered.User;
    }

    private static async Task<(AdminUser User, string Token)> RegisterAsync(HttpClient client, string? email = null)
    {
        email ??= $"user-{Guid.NewGuid():N}@example.com";
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "123456"));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        response.StatusCode.Should().Be(HttpStatusCode.Created);
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
        var databaseName = $"project-tests-{Guid.NewGuid():N}";
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
