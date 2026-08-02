using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using WEB_Demo.Api.Options;

namespace WEB_Demo.Api.Services;

public sealed class OpenAiCompatibleEmbeddingClient : IEmbeddingClient
{
    private readonly IHttpClientFactory _factory;
    private readonly SpecAssistantOptions _options;

    public OpenAiCompatibleEmbeddingClient(IHttpClientFactory factory, IOptions<SpecAssistantOptions> options)
    {
        _factory = factory;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> inputs, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.EmbeddingBaseUrl) || string.IsNullOrWhiteSpace(_options.EmbeddingModel))
            throw new EmbeddingUnavailableException("The embedding model is not configured.");
        if (inputs.Count == 0) return [];

        var results = new List<float[]>();
        try
        {
            foreach (var batch in inputs.Chunk(Math.Max(1, _options.EmbeddingBatchSize)))
            {
                var baseUri = new Uri(_options.EmbeddingBaseUrl.EndsWith('/') ? _options.EmbeddingBaseUrl : _options.EmbeddingBaseUrl + "/");
                using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, "embeddings"))
                { Content = JsonContent.Create(new { model = _options.EmbeddingModel, input = batch }) };
                if (!string.IsNullOrWhiteSpace(_options.EmbeddingApiKey))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.EmbeddingApiKey);
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds)));
                using var response = await _factory.CreateClient(nameof(OpenAiCompatibleEmbeddingClient)).SendAsync(request, timeout.Token);
                if (!response.IsSuccessStatusCode) throw new EmbeddingUnavailableException("The embedding service is temporarily unavailable.");
                using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(timeout.Token), cancellationToken: timeout.Token);
                var data = json.RootElement.GetProperty("data").EnumerateArray()
                    .OrderBy(item => item.GetProperty("index").GetInt32()).ToArray();
                if (data.Length != batch.Length) throw new EmbeddingUnavailableException("The embedding service returned an incomplete response.");
                results.AddRange(data.Select(item => item.GetProperty("embedding").EnumerateArray().Select(number => number.GetSingle()).ToArray()));
            }
            VectorMath.ValidateCollection(results);
            return results;
        }
        catch (EmbeddingUnavailableException) { throw; }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException or UriFormatException)
        { throw new EmbeddingUnavailableException("The embedding service is temporarily unavailable.", exception); }
    }
}
