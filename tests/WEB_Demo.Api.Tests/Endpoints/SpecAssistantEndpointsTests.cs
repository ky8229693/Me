using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using WEB_Demo.Api.Data;
using WEB_Demo.Api.Models;
using WEB_Demo.Api.Services;

namespace WEB_Demo.Api.Tests.Endpoints;

public sealed class SpecAssistantEndpointsTests
{
    [Fact]
    public async Task Query_RequiresAuthentication()
    {
        await using var factory = CreateFactory();
        var response = await factory.CreateClient().PostAsJsonAsync("/api/spec-assistant/query", new { question = "Issue permission" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task IndexStatus_RequiresAuthenticationAndReturnsSafeMetadata()
    {
        await using var factory = CreateFactory();
        (await factory.CreateClient().GetAsync("/api/spec-assistant/index")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var client = await CreateAuthenticatedClientAsync(factory);

        var response = await client.GetAsync("/api/spec-assistant/index");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().MatchRegex("\\\"retrievalMode\\\":\\\"(Embedding|Hybrid)\\\"").And.Contain("Unbuilt");
        content.Should().NotContain("ApiKey").And.NotContain("vector");
    }

    [Fact]
    public async Task RebuildIndex_ReturnsReadySnapshot()
    {
        await using var factory = CreateFactory(index: new StubIndex());
        var client = await CreateAuthenticatedClientAsync(factory);

        var response = await client.PostAsync("/api/spec-assistant/index/rebuild", null);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SpecIndexStatusResponse>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Data!.Status.Should().Be("Ready");
        body.Data.ChunkCount.Should().Be(1);
    }

    [Fact]
    public async Task Query_ValidatesQuestionBeforeCallingServices()
    {
        var assistant = new StubAssistant("answer");
        await using var factory = CreateFactory(assistant);
        var client = await CreateAuthenticatedClientAsync(factory);

        var response = await client.PostAsJsonAsync("/api/spec-assistant/query", new { question = " " });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        assistant.Calls.Should().Be(0);
    }

    [Fact]
    public async Task Query_ReturnsGroundedAnswerAndBackendSources()
    {
        var assistant = new StubAssistant("Use the issue specification.");
        await using var factory = CreateFactory(assistant);
        var client = await CreateAuthenticatedClientAsync(factory);

        var response = await client.PostAsJsonAsync("/api/spec-assistant/query", new { question = "Issue permission" });
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SpecAssistantQueryResponse>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Data!.Answer.Should().Be("Use the issue specification.");
        body.Data.RelatedSpecs.Should().Equal("issue-management");
        body.Data.Sources.Should().Equal("openspec/specs/issue-management/spec.md");
    }

    [Fact]
    public async Task Query_MapsAssistantFailureToSafeServiceUnavailableResponse()
    {
        await using var factory = CreateFactory(new StubAssistant(exception: new SpecAssistantUnavailableException("secret upstream detail")));
        var client = await CreateAuthenticatedClientAsync(factory);

        var response = await client.PostAsJsonAsync("/api/spec-assistant/query", new { question = "Issue permission" });
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        content.Should().Contain("SPEC_ASSISTANT_UNAVAILABLE").And.NotContain("secret upstream detail");
    }

    private static async Task<HttpClient> CreateAuthenticatedClientAsync(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest($"spec-{Guid.NewGuid():N}@example.com", "123456"));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Data!.AccessToken);
        return client;
    }

    private static WebApplicationFactory<Program> CreateFactory(StubAssistant? assistant = null, ISpecVectorIndex? index = null)
    {
        var databaseName = $"spec-assistant-tests-{Guid.NewGuid():N}";
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["SpecAssistant:VectorStore"] = "Memory" }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.RemoveAll<ISpecDocumentRetriever>();
                services.AddSingleton<ISpecDocumentRetriever>(new StubRetriever());
                services.RemoveAll<ISpecAssistantClient>();
                services.AddSingleton<ISpecAssistantClient>(assistant ?? new StubAssistant("answer"));
                if (index is not null)
                {
                    services.RemoveAll<ISpecVectorIndex>();
                    services.AddSingleton(index);
                }
            });
        });
    }

    private sealed class StubIndex : ISpecVectorIndex
    {
        private SpecIndexSnapshot? _snapshot;
        public SpecIndexSnapshot? Snapshot => _snapshot;
        public string Status => _snapshot is null ? "Unbuilt" : "Ready";
        public Task<SpecIndexSnapshot> GetOrBuildAsync(CancellationToken cancellationToken = default) => RebuildAsync(cancellationToken);
        public Task<SpecIndexSnapshot> RebuildAsync(CancellationToken cancellationToken = default)
        {
            _snapshot = new SpecIndexSnapshot([
                new SpecVectorEntry(new SpecDocumentChunk("issues", "source", "content", 1, "one"), [1, 0])
            ], "embed", DateTimeOffset.UtcNow);
            return Task.FromResult(_snapshot);
        }
    }

    private sealed class StubRetriever : ISpecDocumentRetriever
    {
        public Task<SpecRetrievalResult> RetrieveAsync(string question, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SpecRetrievalResult([
                new SpecDocumentChunk("issue-management", "openspec/specs/issue-management/spec.md", "Issue permission rules", 2)
            ]));
    }

    private sealed class StubAssistant(string? answer = null, Exception? exception = null) : ISpecAssistantClient
    {
        public int Calls { get; private set; }
        public Task<string> AnswerAsync(string question, IReadOnlyList<SpecDocumentChunk> context, CancellationToken cancellationToken = default)
        {
            Calls++;
            return exception is null ? Task.FromResult(answer!) : Task.FromException<string>(exception);
        }

        public Task<ProjectDraftFields> CreateProjectDraftAsync(string prompt, CancellationToken cancellationToken = default) =>
            exception is null
                ? Task.FromResult(new ProjectDraftFields("Test project", null))
                : Task.FromException<ProjectDraftFields>(exception);
    }
}
