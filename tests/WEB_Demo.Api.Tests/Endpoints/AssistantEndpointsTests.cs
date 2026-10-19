using System.Net;
using System.Net.Http.Headers;
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

public sealed class AssistantEndpointsTests
{
    [Fact]
    public async Task Conversations_RequireAuthentication()
    {
        await using var factory = CreateFactory();
        var response = await factory.CreateClient().GetAsync("/api/assistant/conversations");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Conversation_PersistsAndIsIsolatedByUser()
    {
        await using var factory = CreateFactory();
        var owner = await CreateAuthenticatedClientAsync(factory);
        var outsider = await CreateAuthenticatedClientAsync(factory);

        var create = await owner.PostAsJsonAsync("/api/assistant/conversations", new { title = "My assistant" });
        var created = await create.Content.ReadFromJsonAsync<ApiResponse<AssistantConversationInfo>>();

        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = created!.Data!.Id;
        (await owner.GetAsync($"/api/assistant/conversations/{id}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await outsider.GetAsync($"/api/assistant/conversations/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        var list = await owner.GetFromJsonAsync<ApiResponse<IReadOnlyCollection<AssistantConversationSummary>>>(
            "/api/assistant/conversations");
        list!.Data.Should().ContainSingle(item => item.Id == id);
    }

    [Fact]
    public async Task Context_RejectsProjectThatUserCannotAccess()
    {
        await using var factory = CreateFactory();
        var outsider = await CreateAuthenticatedClientAsync(factory);
        var project = new Project
        {
            Name = "Private project", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        };
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Projects.Add(project);
            await db.SaveChangesAsync();
        }

        var response = await outsider.PostAsJsonAsync("/api/assistant/conversations",
            new { projectId = project.Id });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<HttpClient> CreateAuthenticatedClientAsync(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest($"assistant-{Guid.NewGuid():N}@example.com", "123456"));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Data!.AccessToken);
        return client;
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        var databaseName = $"assistant-tests-{Guid.NewGuid():N}";
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
