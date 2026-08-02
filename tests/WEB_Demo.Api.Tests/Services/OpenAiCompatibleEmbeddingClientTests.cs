using System.Net;
using System.Text;
using FluentAssertions;
using WEB_Demo.Api.Options;
using WEB_Demo.Api.Services;

namespace WEB_Demo.Api.Tests.Services;

public sealed class OpenAiCompatibleEmbeddingClientTests
{
    [Fact]
    public async Task EmbedAsync_AllowsLocalProviderWithoutAuthorizationAndOrdersVectors()
    {
        var handler = new StubHandler(request =>
        {
            request.Headers.Authorization.Should().BeNull();
            request.RequestUri!.ToString().Should().Be("http://localhost:11434/v1/embeddings");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":[{\"index\":1,\"embedding\":[0,1]},{\"index\":0,\"embedding\":[1,0]}]}", Encoding.UTF8, "application/json")
            };
        });
        var client = CreateClient(handler);

        var vectors = await client.EmbedAsync(["one", "two"]);

        vectors[0].Should().Equal(1, 0);
        vectors[1].Should().Equal(0, 1);
    }

    [Fact]
    public async Task EmbedAsync_RejectsIncompleteOrInvalidVectors()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"data\":[{\"index\":0,\"embedding\":[0,0]}]}", Encoding.UTF8, "application/json")
        });

        var action = () => CreateClient(handler).EmbedAsync(["one"]);

        await action.Should().ThrowAsync<EmbeddingUnavailableException>();
    }

    private static OpenAiCompatibleEmbeddingClient CreateClient(HttpMessageHandler handler) => new(
        new StubFactory(new HttpClient(handler)),
        Microsoft.Extensions.Options.Options.Create(new SpecAssistantOptions
        { EmbeddingBaseUrl = "http://localhost:11434/v1/", EmbeddingModel = "embed", EmbeddingBatchSize = 8 }));

    private sealed class StubFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(response(request));
    }
}
