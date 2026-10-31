using FluentAssertions;
using Pgvector;
using WEB_Demo.Api.Data;
using WEB_Demo.Api.Models;
using WEB_Demo.Api.Services;

namespace WEB_Demo.Api.Tests.Services;

public sealed class SpecVectorDeltaPlannerTests
{
    [Fact]
    public void Plan_SeparatesChangedStaleAndUnchangedChunks()
    {
        var unchanged = Chunk("one", "same");
        var changed = Chunk("two", "new content");
        var existing = new Dictionary<string, SpecVectorDocument>
        {
            ["one"] = Row("one", "same", 1024),
            ["two"] = Row("two", "old content", 1024),
            ["stale"] = Row("stale", "removed", 1024)
        };

        var result = SpecVectorDeltaPlanner.Plan([unchanged, changed], existing, 1024);

        result.Changed.Should().ContainSingle(item => item.Id == "two");
        result.Stale.Should().ContainSingle(item => item.ChunkId == "stale");
        result.Unchanged.Should().Be(1);
    }

    [Fact]
    public void Plan_RequiresResyncWhenDimensionChanges()
    {
        var chunk = Chunk("one", "same");
        var result = SpecVectorDeltaPlanner.Plan([chunk],
            new Dictionary<string, SpecVectorDocument> { ["one"] = Row("one", "same", 768) }, 1024);
        result.Changed.Should().ContainSingle();
        result.Unchanged.Should().Be(0);
    }

    private static SpecDocumentChunk Chunk(string id, string content) =>
        new("capability", "openspec/specs/capability/spec.md", content, 0, id);

    private static SpecVectorDocument Row(string id, string content, int dimension) => new()
    {
        ChunkId = id, ContentHash = SpecVectorDeltaPlanner.Hash(content), Dimension = dimension,
        EmbeddingModel = "bge-m3", Embedding = new Vector(new float[1024])
    };
}
