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

    private static WebApplicationFactory<Program> CreateFactory(StubAssistant? assistant = null)
    {
        var databaseName = $"spec-assistant-tests-{Guid.NewGuid():N}";
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.RemoveAll<ISpecDocumentRetriever>();
                services.AddSingleton<ISpecDocumentRetriever>(new StubRetriever());
                services.RemoveAll<ISpecAssistantClient>();
                services.AddSingleton<ISpecAssistantClient>(assistant ?? new StubAssistant("answer"));
            });
        });
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
    }
}
