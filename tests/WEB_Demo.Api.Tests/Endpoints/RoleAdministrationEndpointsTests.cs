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

public sealed class RoleAdministrationEndpointsTests
{
    [Fact]
    public async Task Roles_WithUserToken_ReturnsForbidden()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var (_, token) = await RegisterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        (await client.GetAsync("/api/admin/roles")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RoleCrud_AssignmentAndRename_EnforcesSafetyAndRevokesToken()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var admin = await CreateAdminAsync(factory, client);
        var (target, oldToken) = await RegisterAsync(client);
        await LoginAsAsync(client, admin.Email);

        var create = await client.PostAsJsonAsync("/api/admin/roles", new CreateRoleRequest(" 客服人員 "));
        var created = await create.Content.ReadFromJsonAsync<ApiResponse<AdminRole>>();
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        created!.Data!.Name.Should().Be("客服人員");
        (await client.PostAsJsonAsync("/api/admin/roles", new CreateRoleRequest("客服人員")))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);

        var assign = await client.PutAsJsonAsync($"/api/admin/users/{target.Id}/roles",
            new UpdateUserRolesRequest([AppRoles.User, "客服人員"]));
        assign.StatusCode.Should().Be(HttpStatusCode.OK);

        (await client.DeleteAsync($"/api/admin/roles/{created.Data.Id}"))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);

        var reloginToken = await LoginAsAsync(client, target.Email);
        await LoginAsAsync(client, admin.Email);
        var targetClient = factory.CreateClient();
        targetClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", reloginToken);
        var invalidRename = await client.PutAsJsonAsync($"/api/admin/roles/{created.Data.Id}",
            new UpdateRoleRequest("A!"));
        invalidRename.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await targetClient.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.OK);

        var rename = await client.PutAsJsonAsync($"/api/admin/roles/{created.Data.Id}",
            new UpdateRoleRequest("Support_Team"));
        rename.StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/api/admin/status")).StatusCode.Should().Be(HttpStatusCode.OK);

        (await targetClient.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        await LoginAsAsync(client, admin.Email);
        (await client.PutAsJsonAsync($"/api/admin/users/{target.Id}/roles",
            new UpdateUserRolesRequest([AppRoles.User]))).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.DeleteAsync($"/api/admin/roles/{created.Data.Id}"))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var listJson = await (await client.GetAsync("/api/admin/roles")).Content.ReadAsStringAsync();
        listJson.Should().NotContain("normalizedName").And.NotContain("concurrencyStamp");
    }

    [Fact]
    public async Task Roles_WithAssignedPermissions_ReturnsPermissionSummary()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var admin = await CreateAdminAsync(factory, client);
        await LoginAsAsync(client, admin.Email);

        var create = await client.PostAsJsonAsync("/api/admin/roles", new CreateRoleRequest("ProjectReader"));
        var created = await create.Content.ReadFromJsonAsync<ApiResponse<AdminRole>>();
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        var update = await client.PutAsJsonAsync(
            $"/api/admin/roles/{created!.Data!.Id}/permissions",
            new UpdateRolePermissionsRequest([AppPermissions.RolesRead]));
        update.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await client.GetAsync("/api/admin/roles");
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyCollection<AdminRole>>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Data!.Single(role => role.Id == created.Data.Id).Permissions
            .Should().BeEquivalentTo([AppPermissions.RolesRead]);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("A!")]
    [InlineData("A")]
    public async Task CreateRole_WithReservedOrInvalidName_ReturnsBadRequest(string name)
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var admin = await CreateAdminAsync(factory, client);
        await LoginAsAsync(client, admin.Email);

        (await client.PostAsJsonAsync("/api/admin/roles", new CreateRoleRequest(name)))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task<AdminUser> CreateAdminAsync(WebApplicationFactory<Program> factory, HttpClient client)
    {
        var (user, _) = await RegisterAsync(client, $"admin-{Guid.NewGuid():N}@example.com");
        await using var scope = factory.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var identityUser = await manager.FindByIdAsync(user.Id);
        (await manager.AddToRoleAsync(identityUser!, AppRoles.Admin)).Succeeded.Should().BeTrue();
        (await manager.UpdateSecurityStampAsync(identityUser!)).Succeeded.Should().BeTrue();
        return user;
    }

    private static async Task<(AdminUser User, string Token)> RegisterAsync(HttpClient client, string? email = null)
    {
        email ??= $"user-{Guid.NewGuid():N}@example.com";
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "123456"));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        return (new AdminUser(body!.Data!.User.Id, email, true, body.Data.User.Roles), body.Data.AccessToken);
    }

    private static async Task<string> LoginAsAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "123456"));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Data!.AccessToken);
        return body.Data.AccessToken;
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        var databaseName = $"admin-roles-tests-{Guid.NewGuid():N}";
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
