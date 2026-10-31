using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using WEB_Demo.Api.Data;
using WEB_Demo.Api.Models;
using WEB_Demo.Api.Options;

namespace WEB_Demo.Api.Services;

public sealed class PostgresSpecVectorStore(
    AppDbContext db,
    ISpecDocumentSource source,
    IEmbeddingClient embeddings,
    IOptions<SpecAssistantOptions> options) : IPersistentSpecVectorStore
{
    private readonly SpecAssistantOptions _options = options.Value;

    public async Task<(int Count, DateTimeOffset? SyncedAt)> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var compatible = db.SpecVectorDocuments.Where(item =>
            item.EmbeddingModel == _options.EmbeddingModel && item.Dimension == _options.EmbeddingDimension);
        return (await compatible.CountAsync(cancellationToken), await compatible.MaxAsync(item => (DateTimeOffset?)item.UpdatedAt, cancellationToken));
    }

    public async Task<SpecVectorSyncResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var chunks = await source.LoadAsync(cancellationToken);
            var existing = await db.SpecVectorDocuments
                .Where(item => item.EmbeddingModel == _options.EmbeddingModel)
                .ToDictionaryAsync(item => item.ChunkId, cancellationToken);
            var delta = SpecVectorDeltaPlanner.Plan(chunks, existing, _options.EmbeddingDimension);
            var changed = delta.Changed.ToArray();
            var vectors = changed.Length == 0
                ? []
                : await embeddings.EmbedAsync(changed.Select(item => item.Content).ToArray(), cancellationToken);
            VectorMath.ValidateCollection(vectors);
            if (vectors.Any(vector => vector.Length != _options.EmbeddingDimension))
                throw new EmbeddingUnavailableException("Embedding dimension does not match the configured vector store.");

            var stale = delta.Stale.ToArray();
            var now = DateTimeOffset.UtcNow;
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            db.SpecVectorDocuments.RemoveRange(stale);
            var inserted = 0;
            var updated = 0;
            for (var index = 0; index < changed.Length; index++)
            {
                var chunk = changed[index];
                if (!existing.TryGetValue(chunk.Id, out var row))
                {
                    row = new SpecVectorDocument { ChunkId = chunk.Id, EmbeddingModel = _options.EmbeddingModel };
                    db.SpecVectorDocuments.Add(row);
                    inserted++;
                }
                else updated++;
                row.Capability = chunk.Capability;
                row.SourcePath = chunk.SourcePath;
                row.Content = chunk.Content;
                row.ContentHash = SpecVectorDeltaPlanner.Hash(chunk.Content);
                row.Dimension = _options.EmbeddingDimension;
                row.Embedding = new Vector(vectors[index]);
                row.UpdatedAt = now;
            }
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new SpecVectorSyncResult(inserted, updated, stale.Length, delta.Unchanged, chunks.Count, now);
        }
        catch (EmbeddingUnavailableException) { throw; }
        catch (Exception exception) when (exception is DbUpdateException or InvalidOperationException)
        { throw new EmbeddingUnavailableException("The persistent specification index is unavailable.", exception); }
    }

    public async Task<IReadOnlyList<SpecDocumentChunk>> QueryAsync(float[] query, CancellationToken cancellationToken = default)
    {
        if (query.Length != _options.EmbeddingDimension)
            throw new EmbeddingUnavailableException("Query embedding dimension does not match the vector store.");
        var vector = new Vector(query);
        var limit = Math.Max(1, _options.TopK);
        var rows = await db.SpecVectorDocuments
            .Where(item => item.EmbeddingModel == _options.EmbeddingModel && item.Dimension == _options.EmbeddingDimension)
            .Select(item => new { Item = item, Distance = item.Embedding.CosineDistance(vector) })
            .Where(result => 1d - result.Distance >= _options.MinimumSimilarity)
            .OrderBy(result => result.Distance)
            .Take(limit)
            .ToArrayAsync(cancellationToken);
        return rows.Select(result => new SpecDocumentChunk(result.Item.Capability, result.Item.SourcePath,
            result.Item.Content, 1d - result.Distance, result.Item.ChunkId)).ToArray();
    }

}
