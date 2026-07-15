using System.Net;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Net.Http.Headers;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using WEB_Demo.Api.Data;
using WEB_Demo.Api.Models;
using WEB_Demo.Api.Services;

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
        var jwtId = token.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Jti).Value;

        await using var scope = factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = await userManager.FindByEmailAsync(email);
        user.Should().NotBeNull();
        (await userManager.IsInRoleAsync(user!, "User")).Should().BeTrue();
        var registry = scope.ServiceProvider.GetRequiredService<IJwtSessionRegistry>();
        registry.IsActive(jwtId, user!.Id).Should().BeTrue();
    }

    [Fact]
    public async Task CurrentUser_AfterDirectSecurityStampChange_RemainsValidUntilSessionIsRevoked()
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

        (await client.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.OK);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(registerBody.Data.AccessToken);
        var userId = token.Claims.Single(claim => claim.Type == ClaimTypes.NameIdentifier).Value;
        factory.Services.GetRequiredService<IJwtSessionRegistry>().RevokeAll(userId);

        (await client.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CurrentUser_WhenTokenComesFromRestartedRegistry_ReturnsUnauthorized()
    {
        await using var originalFactory = CreateFactory();
        var originalClient = originalFactory.CreateClient();
        var email = $"restart-{Guid.NewGuid():N}@example.com";
        var register = await originalClient.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "123456"));
        var body = await register.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();

        await using var restartedFactory = CreateFactory();
        var restartedClient = restartedFactory.CreateClient();
        restartedClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body!.Data!.AccessToken);

        (await restartedClient.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CurrentUser_WithCryptographicallyValidTokenMissingJti_ReturnsUnauthorized()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "user-1"),
            new Claim(ClaimTypes.NameIdentifier, "user-1")
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            "local-development-signing-key-for-web-demo-auth-foundation"));
        var token = new JwtSecurityToken(
            issuer: "WEB_Demo.Api",
            audience: "WEB_Demo.Client",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            new JwtSecurityTokenHandler().WriteToken(token));

        (await client.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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

    [Fact]
    public async Task Logout_RevokesOnlyTheCurrentSession()
    {
        await using var factory = CreateFactory();
        var firstClient = factory.CreateClient();
        var email = $"logout-{Guid.NewGuid():N}@example.com";
        var register = await firstClient.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "123456"));
        var firstAuth = await register.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        firstClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", firstAuth!.Data!.AccessToken);

        var secondClient = factory.CreateClient();
        var login = await secondClient.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, "123456"));
        var secondAuth = await login.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        secondClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", secondAuth!.Data!.AccessToken);

        (await firstClient.PostAsync("/api/auth/logout", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await firstClient.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await secondClient.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_WithoutBearerToken_ReturnsUnauthorized()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        (await client.PostAsync("/api/auth/logout", null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
