using WEB_Demo.Api.Models;

namespace WEB_Demo.Api.Services;

public sealed class PostgresEmbeddingSpecDocumentRetriever(
    IPersistentSpecVectorStore store,
    IEmbeddingClient embeddings) : ISpecDocumentRetriever
{
    public async Task<SpecRetrievalResult> RetrieveAsync(string question, CancellationToken cancellationToken = default)
    {
        var status = await store.GetStatusAsync(cancellationToken);
        if (status.Count == 0) await store.SyncAsync(cancellationToken);
        var query = (await embeddings.EmbedAsync([question], cancellationToken)).Single();
        return new SpecRetrievalResult(await store.QueryAsync(query, cancellationToken));
    }
}
