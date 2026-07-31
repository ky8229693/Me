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

public sealed class AuthorizationEndpointsTests
{
    [Fact]
    public async Task AdminStatus_WithoutToken_ReturnsUnauthorizedEnvelope()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/admin/status");
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        body.Should().NotBeNull();
        body!.Success.Should().BeFalse();
        body.ErrorCode.Should().Be(ApiErrorCodes.Unauthorized);
    }

    [Fact]
    public async Task AdminStatus_WithUserRole_ReturnsForbiddenEnvelope()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var auth = await RegisterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var response = await client.GetAsync("/api/admin/status");
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        body.Should().NotBeNull();
        body!.Success.Should().BeFalse();
        body.ErrorCode.Should().Be(ApiErrorCodes.Forbidden);
    }

    [Fact]
    public async Task AdminStatus_WithAdminRole_ReturnsSuccessEnvelope()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var email = $"admin-{Guid.NewGuid():N}@example.com";
        const string password = "123456";

        await RegisterAsync(client, email, password);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = await userManager.FindByEmailAsync(email);
            user.Should().NotBeNull();
            (await userManager.AddToRoleAsync(user!, AppRoles.Admin)).Succeeded.Should().BeTrue();
        }

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        loginBody.Should().NotBeNull();
        loginBody!.Success.Should().BeTrue();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginBody.Data!.AccessToken);

        var response = await client.GetAsync("/api/admin/status");
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task RoleInitializer_WhenRunTwice_DoesNotCreateDuplicateRoles()
    {
        await using var factory = CreateFactory();
        _ = factory.CreateClient();

        await using var scope = factory.Services.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<AppRoleInitializer>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await initializer.InitializeAsync();
        await initializer.InitializeAsync();

        var roleNames = await roleManager.Roles.Select(role => role.Name).ToListAsync();
        roleNames.Should().Contain(AppRoles.Admin);
        roleNames.Should().Contain(AppRoles.User);
        roleNames.Count(name => name == AppRoles.Admin).Should().Be(1);
        roleNames.Count(name => name == AppRoles.User).Should().Be(1);
    }

    [Fact]
    public async Task PermissionInitializer_WhenRunTwice_KeepsCatalogUniqueAndAdminComplete()
    {
        await using var factory = CreateFactory();
        _ = factory.CreateClient();

        await using var scope = factory.Services.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<PermissionCatalogInitializer>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await initializer.InitializeAsync();
        await initializer.InitializeAsync();

        (await dbContext.Permissions.Select(item => item.Key).ToArrayAsync())
            .Should().BeEquivalentTo(AppPermissions.AllKeys);
        var admin = await roleManager.FindByNameAsync(AppRoles.Admin);
        (await dbContext.RolePermissions.Where(item => item.RoleId == admin!.Id).CountAsync())
            .Should().Be(AppPermissions.AllKeys.Count);
    }

    private static async Task<AuthResponse> RegisterAsync(
        HttpClient client,
        string? email = null,
        string password = "123456")
    {
        email ??= $"user-{Guid.NewGuid():N}@example.com";
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();

        return body.Data!;
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        var databaseName = $"authorization-tests-{Guid.NewGuid():N}";

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<AppDbContext>>();
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase(databaseName));
                });
            });
    }
}
