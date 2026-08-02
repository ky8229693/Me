using Microsoft.Extensions.Options;
using WEB_Demo.Api.Models;
using WEB_Demo.Api.Options;

namespace WEB_Demo.Api.Services;

public sealed class EmbeddingSpecDocumentRetriever : ISpecDocumentRetriever
{
    private readonly ISpecVectorIndex _index;
    private readonly IEmbeddingClient _embeddings;
    private readonly SpecAssistantOptions _options;
    public EmbeddingSpecDocumentRetriever(ISpecVectorIndex index, IEmbeddingClient embeddings, IOptions<SpecAssistantOptions> options)
    { _index = index; _embeddings = embeddings; _options = options.Value; }

    public async Task<SpecRetrievalResult> RetrieveAsync(string question, CancellationToken cancellationToken = default)
    {
        var snapshot = await _index.GetOrBuildAsync(cancellationToken);
        var query = (await _embeddings.EmbedAsync([question], cancellationToken)).Single();
        var chunks = snapshot.Entries.Select(entry => entry.Chunk with { Score = VectorMath.CosineSimilarity(query, entry.Vector) })
            .Where(chunk => chunk.Score >= _options.MinimumSimilarity)
            .OrderByDescending(chunk => chunk.Score).ThenBy(chunk => chunk.SourcePath, StringComparer.Ordinal)
            .Take(Math.Max(1, _options.TopK)).ToArray();
        return new SpecRetrievalResult(chunks);
    }
}
