using Microsoft.Extensions.Options;
using WEB_Demo.Api.Models;
using WEB_Demo.Api.Options;

namespace WEB_Demo.Api.Services;

public sealed class ConfiguredSpecDocumentRetriever : ISpecDocumentRetriever
{
    private readonly LexicalSpecDocumentRetriever _lexical;
    private readonly EmbeddingSpecDocumentRetriever _embedding;
    private readonly PostgresEmbeddingSpecDocumentRetriever _postgres;
    private readonly SpecAssistantOptions _options;
    public ConfiguredSpecDocumentRetriever(LexicalSpecDocumentRetriever lexical, EmbeddingSpecDocumentRetriever embedding,
        PostgresEmbeddingSpecDocumentRetriever postgres, IOptions<SpecAssistantOptions> options)
    { _lexical = lexical; _embedding = embedding; _postgres = postgres; _options = options.Value; }
    public async Task<SpecRetrievalResult> RetrieveAsync(string question, CancellationToken cancellationToken = default)
    {
        if (_options.RetrievalMode.Equals("Lexical", StringComparison.OrdinalIgnoreCase))
            return await _lexical.RetrieveAsync(question, cancellationToken);
        ISpecDocumentRetriever semantic = _options.VectorStore.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase)
            ? _postgres
            : _embedding;
        if (!_options.RetrievalMode.Equals("Hybrid", StringComparison.OrdinalIgnoreCase))
            return await semantic.RetrieveAsync(question, cancellationToken);

        var lexicalTask = _lexical.RetrieveAsync(question, cancellationToken);
        var embeddingTask = semantic.RetrieveAsync(question, cancellationToken);
        await Task.WhenAll(lexicalTask, embeddingTask);

        var limit = Math.Max(_options.MaxCandidateChunks, _options.TopK);
        var chunks = lexicalTask.Result.Chunks.Concat(embeddingTask.Result.Chunks)
            .DistinctBy(chunk => string.IsNullOrWhiteSpace(chunk.Id)
                ? $"{chunk.SourcePath}\n{chunk.Content}"
                : chunk.Id)
            .Take(limit)
            .ToArray();
        return new SpecRetrievalResult(chunks);
    }
}
