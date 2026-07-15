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
using WEB_Demo.Api.Services;

namespace WEB_Demo.Api.Tests.Endpoints;

public sealed class AccountAdministrationEndpointsTests
{
    [Fact]
    public async Task Users_WithUserRole_ReturnsForbidden()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var (_, token) = await RegisterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Users_WithAdminRole_ReturnsSearchablePageWithoutSensitiveFields()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var (admin, _) = await CreateAdminAsync(factory, client);
        var marker = Guid.NewGuid().ToString("N");
        var (target, _) = await RegisterAsync(client, $"search-{marker}@example.com");
        await LoginAsAsync(client, admin.Email);

        var response = await client.GetAsync($"/api/admin/users?page=1&pageSize=10&search={marker}");
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<AdminUser>>>();
        var json = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Data!.Items.Should().ContainSingle(user => user.Id == target.Id);
        json.Should().NotContain("passwordHash").And.NotContain("securityStamp");
    }

    [Fact]
    public async Task UserDetail_WhenMissing_ReturnsNotFoundEnvelope()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var (admin, _) = await CreateAdminAsync(factory, client);
        await LoginAsAsync(client, admin.Email);

        var response = await client.GetAsync($"/api/admin/users/{Guid.NewGuid():N}");
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AdminUser>>();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body!.ErrorCode.Should().Be(ApiErrorCodes.NotFound);
    }

    [Fact]
    public async Task DisableUser_RevokesExistingToken_AndCanBeEnabledAgain()
    {
        await using var factory = CreateFactory();
        var adminClient = factory.CreateClient();
        var (admin, _) = await CreateAdminAsync(factory, adminClient);
        var (target, targetToken) = await RegisterAsync(adminClient);
        await LoginAsAsync(adminClient, admin.Email);

        var disable = await adminClient.PutAsJsonAsync(
            $"/api/admin/users/{target.Id}/status",
            new UpdateAccountStatusRequest(false));
        disable.StatusCode.Should().Be(HttpStatusCode.OK);

        var targetClient = factory.CreateClient();
        targetClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", targetToken);
        (await targetClient.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var enable = await adminClient.PutAsJsonAsync(
            $"/api/admin/users/{target.Id}/status",
            new UpdateAccountStatusRequest(true));
        enable.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DisableSelf_ReturnsConflict()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var (admin, _) = await CreateAdminAsync(factory, client);
        await LoginAsAsync(client, admin.Email);

        var response = await client.PutAsJsonAsync(
            $"/api/admin/users/{admin.Id}/status",
            new UpdateAccountStatusRequest(false));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SetRoles_PromotesUserAndRevokesOldToken()
    {
        await using var factory = CreateFactory();
        var adminClient = factory.CreateClient();
        var (admin, _) = await CreateAdminAsync(factory, adminClient);
        var (target, oldToken) = await RegisterAsync(adminClient);
        await LoginAsAsync(adminClient, admin.Email);

        var response = await adminClient.PutAsJsonAsync(
            $"/api/admin/users/{target.Id}/roles",
            new UpdateUserRolesRequest([AppRoles.User, AppRoles.Admin]));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AdminUser>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Data!.Roles.Should().BeEquivalentTo([AppRoles.Admin, AppRoles.User]);

        var oldClient = factory.CreateClient();
        oldClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oldToken);
        (await oldClient.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SetRoles_WhenRolesAreUnchanged_PreservesExistingSession()
    {
        await using var factory = CreateFactory();
        var adminClient = factory.CreateClient();
        var (admin, _) = await CreateAdminAsync(factory, adminClient);
        var (target, targetToken) = await RegisterAsync(adminClient);
        await LoginAsAsync(adminClient, admin.Email);

        var response = await adminClient.PutAsJsonAsync(
            $"/api/admin/users/{target.Id}/roles",
            new UpdateUserRolesRequest([AppRoles.User]));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var targetClient = factory.CreateClient();
        targetClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", targetToken);
        (await targetClient.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("Admin")]
    public async Task SetRoles_WithInvalidSet_ReturnsBadRequest(string role)
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var (admin, _) = await CreateAdminAsync(factory, client);
        var (target, _) = await RegisterAsync(client);
        await LoginAsAsync(client, admin.Email);

        var response = await client.PutAsJsonAsync(
            $"/api/admin/users/{target.Id}/roles",
            new UpdateUserRolesRequest([role]));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetRoles_WhenRemovingOwnAdminRole_ReturnsConflict()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var (admin, _) = await CreateAdminAsync(factory, client);
        await LoginAsAsync(client, admin.Email);

        var response = await client.PutAsJsonAsync(
            $"/api/admin/users/{admin.Id}/roles",
            new UpdateUserRolesRequest([AppRoles.User]));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Service_WhenRemovingLastAdmin_ReturnsConflict()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var (admin, _) = await CreateAdminAsync(factory, client);
        await using var scope = factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<AccountAdministrationService>();

        var result = await service.SetRolesAsync("different-actor", admin.Id, [AppRoles.User]);

        result.Status.Should().Be(AdministrationResultStatus.Conflict);
    }

    private static async Task<(AdminUser User, string Token)> CreateAdminAsync(
        WebApplicationFactory<Program> factory,
        HttpClient client)
    {
        var (user, _) = await RegisterAsync(client, $"admin-{Guid.NewGuid():N}@example.com");
        await using var scope = factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var identityUser = await userManager.FindByIdAsync(user.Id);
        (await userManager.AddToRoleAsync(identityUser!, AppRoles.Admin)).Succeeded.Should().BeTrue();
        (await userManager.UpdateSecurityStampAsync(identityUser!)).Succeeded.Should().BeTrue();
        var token = await LoginAsAsync(client, user.Email);
        return (user with { Roles = [AppRoles.User, AppRoles.Admin] }, token);
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
        var databaseName = $"admin-users-tests-{Guid.NewGuid():N}";
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
