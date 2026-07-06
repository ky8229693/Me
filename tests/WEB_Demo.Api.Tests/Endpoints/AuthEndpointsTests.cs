using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
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
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<AppDbContext>>();
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase($"auth-tests-{Guid.NewGuid():N}"));
                });
            });
    }
}
