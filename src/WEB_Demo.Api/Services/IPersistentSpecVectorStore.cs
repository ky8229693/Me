using WEB_Demo.Api.Models;

namespace WEB_Demo.Api.Services;

public interface IPersistentSpecVectorStore
{
    Task<(int Count, DateTimeOffset? SyncedAt)> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<SpecVectorSyncResult> SyncAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SpecDocumentChunk>> QueryAsync(float[] query, CancellationToken cancellationToken = default);
}
