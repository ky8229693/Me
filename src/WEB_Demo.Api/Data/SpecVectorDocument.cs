using Pgvector;

namespace WEB_Demo.Api.Data;

public sealed class SpecVectorDocument
{
    public long Id { get; set; }
    public string ChunkId { get; set; } = string.Empty;
    public string Capability { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = string.Empty;
    public int Dimension { get; set; }
    public Vector Embedding { get; set; } = null!;
    public DateTimeOffset UpdatedAt { get; set; }
}
