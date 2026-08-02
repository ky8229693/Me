namespace WEB_Demo.Api.Models;

public sealed record SpecAssistantQueryRequest(string? Question);

public sealed record SpecAssistantQueryResponse(
    string Answer,
    IReadOnlyList<string> RelatedSpecs,
    IReadOnlyList<string> Sources);

public sealed record SpecDocumentChunk(
    string Capability,
    string SourcePath,
    string Content,
    double Score,
    string Id = "");

public sealed record SpecRetrievalResult(IReadOnlyList<SpecDocumentChunk> Chunks)
{
    public bool HasMatches => Chunks.Count > 0;
}

public sealed record SpecVectorEntry(SpecDocumentChunk Chunk, float[] Vector);

public sealed record SpecIndexSnapshot(
    IReadOnlyList<SpecVectorEntry> Entries,
    string Model,
    DateTimeOffset BuiltAt);

public sealed record SpecIndexStatusResponse(
    string RetrievalMode,
    string Status,
    string? Model,
    int ChunkCount,
    DateTimeOffset? BuiltAt,
    string Store = "Memory",
    int Inserted = 0,
    int Updated = 0,
    int Deleted = 0,
    int Unchanged = 0);

public sealed record SpecVectorSyncResult(int Inserted, int Updated, int Deleted, int Unchanged, int Total, DateTimeOffset SyncedAt);
