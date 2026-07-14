using System.Net;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WEB_Demo.Api.Data;
using WEB_Demo.Api.Models;

namespace WEB_Demo.Api.Tests.Endpoints;

public sealed class AuthEndpointsTests
{
    [Fact]
    public async Task Register_WithValidRequest_ReturnsTokenAndUser()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "123456"));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.Data.User.Email.Should().Be(email);
        body.Data.User.Roles.Should().Contain("User");

        var token = new JwtSecurityTokenHandler().ReadJwtToken(body.Data.AccessToken);
        token.Claims.Should().Contain(claim =>
            (claim.Type == ClaimTypes.Role || claim.Type == "role") && claim.Value == "User");

        await using var scope = factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = await userManager.FindByEmailAsync(email);
        user.Should().NotBeNull();
        (await userManager.IsInRoleAsync(user!, "User")).Should().BeTrue();
    }

    [Fact]
    public async Task CurrentUser_AfterSecurityStampChanges_ReturnsUnauthorized()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var email = $"stamp-{Guid.NewGuid():N}@example.com";
        var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "123456"));
        var registerBody = await register.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", registerBody!.Data!.AccessToken);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = await userManager.FindByEmailAsync(email);
            (await userManager.UpdateSecurityStampAsync(user!)).Succeeded.Should().BeTrue();
        }

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WhenAccountIsLockedOut_ReturnsGenericUnauthorized()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var email = $"disabled-{Guid.NewGuid():N}@example.com";
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "123456"));

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = await userManager.FindByEmailAsync(email);
            (await userManager.SetLockoutEnabledAsync(user!, true)).Succeeded.Should().BeTrue();
            (await userManager.SetLockoutEndDateAsync(user!, DateTimeOffset.UtcNow.AddYears(100))).Succeeded.Should().BeTrue();
        }

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "123456"));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        body!.Message.Should().Be("Invalid email or password.");
    }

    [Fact]
    public async Task CurrentUser_WithoutBearerToken_ReturnsUnauthorizedEnvelope()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        body.Should().NotBeNull();
        body!.Success.Should().BeFalse();
        body.ErrorCode.Should().Be(ApiErrorCodes.Unauthorized);
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        var databaseName = $"auth-tests-{Guid.NewGuid():N}";

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
