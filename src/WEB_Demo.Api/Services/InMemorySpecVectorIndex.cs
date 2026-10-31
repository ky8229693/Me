using Microsoft.Extensions.Options;
using WEB_Demo.Api.Models;
using WEB_Demo.Api.Options;

namespace WEB_Demo.Api.Services;

public sealed class InMemorySpecVectorIndex : ISpecVectorIndex
{
    private readonly ISpecDocumentSource _source;
    private readonly IEmbeddingClient _embeddings;
    private readonly SpecAssistantOptions _options;
    private readonly SemaphoreSlim _buildLock = new(1, 1);
    private volatile SpecIndexSnapshot? _snapshot;
    private volatile string _status = "Unbuilt";

    public InMemorySpecVectorIndex(ISpecDocumentSource source, IEmbeddingClient embeddings, IOptions<SpecAssistantOptions> options)
    { _source = source; _embeddings = embeddings; _options = options.Value; }

    public SpecIndexSnapshot? Snapshot => _snapshot;
    public string Status => _status;
    public Task<SpecIndexSnapshot> GetOrBuildAsync(CancellationToken cancellationToken = default) =>
        _snapshot is not null ? Task.FromResult(_snapshot) : BuildAsync(false, cancellationToken);
    public Task<SpecIndexSnapshot> RebuildAsync(CancellationToken cancellationToken = default) => BuildAsync(true, cancellationToken);

    private async Task<SpecIndexSnapshot> BuildAsync(bool force, CancellationToken cancellationToken)
    {
        await _buildLock.WaitAsync(cancellationToken);
        try
        {
            if (!force && _snapshot is not null) return _snapshot;
            _status = "Building";
            var chunks = await _source.LoadAsync(cancellationToken);
            if (chunks.Count == 0) throw new EmbeddingUnavailableException("No OpenSpec chunks are available to index.");
            var vectors = await _embeddings.EmbedAsync(chunks.Select(chunk => chunk.Content).ToArray(), cancellationToken);
            VectorMath.ValidateCollection(vectors);
            var entries = chunks.Zip(vectors, (chunk, vector) => new SpecVectorEntry(chunk, vector)).ToArray();
            var next = new SpecIndexSnapshot(entries, _options.EmbeddingModel, DateTimeOffset.UtcNow);
            _snapshot = next;
            _status = "Ready";
            return next;
        }
        catch
        {
            _status = _snapshot is null ? "Unavailable" : "Ready";
            throw;
        }
        finally { _buildLock.Release(); }
    }
}
