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
using Microsoft.EntityFrameworkCore.Storage;
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
        var familyId = token.Claims.Single(claim => claim.Type == "sid").Value;
        (body.Data.ExpiresAt - DateTimeOffset.UtcNow).Should().BeCloseTo(TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(1));
        response.Headers.GetValues("Set-Cookie").Single().ToLowerInvariant().Should()
            .Contain("httponly")
            .And.Contain("samesite=strict")
            .And.Contain("path=/api/auth");

        await using var scope = factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = await userManager.FindByEmailAsync(email);
        user.Should().NotBeNull();
        (await userManager.IsInRoleAsync(user!, "User")).Should().BeTrue();
        var registry = scope.ServiceProvider.GetRequiredService<IJwtSessionRegistry>();
        registry.IsActive(jwtId, user!.Id, familyId).Should().BeTrue();
    }

    [Fact]
    public async Task Refresh_RotatesCookieAndAccessGeneration()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest($"rotate-{Guid.NewGuid():N}@example.com", "123456"));
        var firstAuth = await register.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        var firstCookie = GetRefreshCookie(register);

        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refreshRequest.Headers.Add("Cookie", firstCookie);
        var refresh = await client.SendAsync(refreshRequest);
        var secondAuth = await refresh.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        var secondCookie = GetRefreshCookie(refresh);

        refresh.StatusCode.Should().Be(HttpStatusCode.OK);
        secondAuth!.Data!.AccessToken.Should().NotBe(firstAuth!.Data!.AccessToken);
        secondCookie.Should().NotBe(firstCookie);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", firstAuth.Data.AccessToken);
        (await client.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", secondAuth.Data.AccessToken);
        (await client.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_WhenConsumedTokenIsReused_RevokesWholeFamily()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest($"reuse-{Guid.NewGuid():N}@example.com", "123456"));
        var oldCookie = GetRefreshCookie(register);

        using var firstRefreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        firstRefreshRequest.Headers.Add("Cookie", oldCookie);
        var firstRefresh = await client.SendAsync(firstRefreshRequest);
        var auth = await firstRefresh.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();

        using var replayRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        replayRequest.Headers.Add("Cookie", oldCookie);
        (await factory.CreateClient().SendAsync(replayRequest)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.Data!.AccessToken);
        (await client.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WhenRequestsRace_AllowsOnlyOneRotation()
    {
        await using var factory = CreateFactory();
        var register = await factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest($"race-{Guid.NewGuid():N}@example.com", "123456"));
        var cookie = GetRefreshCookie(register);

        static HttpRequestMessage RefreshRequest(string cookieHeader)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
            request.Headers.Add("Cookie", cookieHeader);
            return request;
        }

        using var firstRequest = RefreshRequest(cookie);
        using var secondRequest = RefreshRequest(cookie);
        var responses = await Task.WhenAll(
            factory.CreateClient().SendAsync(firstRequest),
            factory.CreateClient().SendAsync(secondRequest));

        responses.Select(response => response.StatusCode).Should()
            .BeEquivalentTo([HttpStatusCode.OK, HttpStatusCode.Unauthorized]);
    }

    [Fact]
    public async Task Register_InProduction_SetsSecureRefreshCookie()
    {
        await using var factory = CreateFactory(environment: "Production");
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest($"secure-{Guid.NewGuid():N}@example.com", "123456"));

        response.Headers.GetValues("Set-Cookie").Single().ToLowerInvariant().Should().Contain("secure");
    }

    [Fact]
    public async Task Register_PersistsOnlyRefreshTokenHash()
    {
        await using var factory = CreateFactory();
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest($"hash-{Guid.NewGuid():N}@example.com", "123456"));
        var rawToken = GetRefreshCookie(response).Split('=', 2)[1];

        await using var scope = factory.Services.CreateAsyncScope();
        var records = await scope.ServiceProvider.GetRequiredService<AppDbContext>()
            .RefreshTokens.AsNoTracking().ToListAsync();
        records.Should().ContainSingle();
        records[0].TokenHash.Should().Be(RefreshTokenCrypto.Hash(rawToken));
        records[0].TokenHash.Should().NotBe(rawToken);
    }

    [Fact]
    public async Task Cleanup_RemovesOnlyRecordsBeyondRetention()
    {
        await using var factory = CreateFactory();
        await factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest($"cleanup-{Guid.NewGuid():N}@example.com", "123456"));

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var record = await dbContext.RefreshTokens.SingleAsync();
        record.ExpiresAt = DateTimeOffset.UtcNow.AddDays(-31);
        await dbContext.SaveChangesAsync();

        (await scope.ServiceProvider.GetRequiredService<IRefreshTokenService>().CleanupAsync()).Should().Be(1);
        (await dbContext.RefreshTokens.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Refresh_AfterApiRestart_RecoversMemorySession()
    {
        var root = new InMemoryDatabaseRoot();
        var databaseName = $"restart-refresh-{Guid.NewGuid():N}";
        await using var originalFactory = CreateFactory(databaseName, root);
        var register = await originalFactory.CreateClient().PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest($"restart-refresh-{Guid.NewGuid():N}@example.com", "123456"));
        var oldAuth = await register.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        var refreshCookie = GetRefreshCookie(register);

        await using var restartedFactory = CreateFactory(databaseName, root);
        var client = restartedFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", oldAuth!.Data!.AccessToken);
        (await client.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        client.DefaultRequestHeaders.Authorization = null;
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", refreshCookie);
        var refresh = await client.SendAsync(request);
        var recovered = await refresh.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        refresh.StatusCode.Should().Be(HttpStatusCode.OK);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", recovered!.Data!.AccessToken);
        (await client.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_WithUnconfiguredOrigin_ReturnsForbidden()
    {
        await using var factory = CreateFactory();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Origin", "http://untrusted.example");

        (await factory.CreateClient().SendAsync(request)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
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
    public async Task Logout_WithoutBearerToken_IsIdempotent()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        (await client.PostAsync("/api/auth/logout", null)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static string GetRefreshCookie(HttpResponseMessage response) =>
        response.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith("web_demo.refresh_token=", StringComparison.Ordinal))
            .Split(';', 2)[0];

    private static WebApplicationFactory<Program> CreateFactory(
        string? sharedDatabaseName = null,
        InMemoryDatabaseRoot? root = null,
        string environment = "Development")
    {
        var databaseName = sharedDatabaseName ?? $"auth-tests-{Guid.NewGuid():N}";

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environment);
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<AppDbContext>>();
                    services.AddDbContext<AppDbContext>(options =>
                    {
                        if (root is null) options.UseInMemoryDatabase(databaseName);
                        else options.UseInMemoryDatabase(databaseName, root);
                    });
                });
            });
    }
}
