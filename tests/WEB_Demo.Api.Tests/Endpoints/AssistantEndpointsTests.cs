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
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using WEB_Demo.Api.Data;
using WEB_Demo.Api.Models;
using WEB_Demo.Api.Services;

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

    [Fact]
    public async Task CreateIssuePrompt_RetriesWithRequiredToolAndReturnsPreviewWithoutCreatingIssue()
    {
        var projectId = Guid.Parse("e0000000-0000-4000-8000-000000000001");
        var chat = new RequiredIssueToolChatService(projectId);
        await using var factory = CreateFactory(new TestAssistantKernelFactory(chat));
        var session = await CreateAuthenticatedSessionAsync(factory);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTimeOffset.UtcNow;
            db.Projects.Add(new Project { Id = projectId, Name = "Test project", CreatedAt = now, UpdatedAt = now });
            db.ProjectMemberships.Add(new ProjectMembership
            {
                ProjectId = projectId, UserId = session.UserId, Role = ProjectRole.Owner,
                CreatedAt = now, UpdatedAt = now
            });
            await db.SaveChangesAsync();
        }
        var created = await (await session.Client.PostAsJsonAsync("/api/assistant/conversations", new { projectId }))
            .Content.ReadFromJsonAsync<ApiResponse<AssistantConversationInfo>>();

        var response = await session.Client.PostAsJsonAsync(
            $"/api/assistant/conversations/{created!.Data!.Id}/messages",
            new { message = "在專案 ID e0000000-0000-4000-8000-000000000001 建立議題，標題「測試議題」、類型 Feature、優先度 Medium。" });
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AssistantTurnResponse>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        chat.Calls.Should().Be(2);
        body!.Data!.State.Should().Be("confirmation_required");
        body.Data.PendingAction.Should().NotBeNull();
        body.Data.PendingAction!.Type.Should().Be("CreateIssue");
        body.Data.Message.Content.Should().Contain("議題已準備完成").And.NotContain("請稍候");
        await using var verifyScope = factory.Services.CreateAsyncScope();
        (await verifyScope.ServiceProvider.GetRequiredService<AppDbContext>().Issues.CountAsync()).Should().Be(0);
    }

    private static async Task<HttpClient> CreateAuthenticatedClientAsync(WebApplicationFactory<Program> factory)
        => (await CreateAuthenticatedSessionAsync(factory)).Client;

    private static async Task<(HttpClient Client, string UserId)> CreateAuthenticatedSessionAsync(
        WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest($"assistant-{Guid.NewGuid():N}@example.com", "123456"));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Data!.AccessToken);
        return (client, body.Data.User.Id);
    }

    private static WebApplicationFactory<Program> CreateFactory(IAssistantKernelFactory? kernelFactory = null)
    {
        var databaseName = $"assistant-tests-{Guid.NewGuid():N}";
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["SpecAssistant:Model"] = "test-model" }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
                if (kernelFactory is not null)
                {
                    services.RemoveAll<IAssistantKernelFactory>();
                    services.AddSingleton(kernelFactory);
                }
            });
        });
    }

    private sealed class TestAssistantKernelFactory(IChatCompletionService chat) : IAssistantKernelFactory
    {
        public Kernel Create(AssistantTools tools)
        {
            var builder = Kernel.CreateBuilder();
            builder.Services.AddSingleton(chat);
            var kernel = builder.Build();
            kernel.Plugins.AddFromObject(tools, "assistant");
            return kernel;
        }
    }

    private sealed class RequiredIssueToolChatService(Guid projectId) : IChatCompletionService
    {
        public int Calls { get; private set; }
        public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

        public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
            ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (Calls == 1)
                return [new ChatMessageContent(AuthorRole.Assistant, "請稍候，我會告訴您結果。")];
            await kernel!.InvokeAsync(kernel.Plugins["assistant"]["prepare_issue_creation"], new KernelArguments
            {
                ["projectId"] = projectId,
                ["title"] = "測試議題",
                ["type"] = "Feature",
                ["priority"] = "Medium"
            }, cancellationToken);
            return [new ChatMessageContent(AuthorRole.Assistant, "工具已完成")];
        }

        public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
            ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
