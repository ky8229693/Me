using FluentAssertions;
using Microsoft.Extensions.Options;
using WEB_Demo.Api.Models;
using WEB_Demo.Api.Options;
using WEB_Demo.Api.Services;

namespace WEB_Demo.Api.Tests.Services;

public sealed class EmbeddingRetrievalTests
{
    [Fact]
    public void CosineSimilarity_RejectsInvalidVectorsAndRanksAlignedVectors()
    {
        VectorMath.CosineSimilarity([1, 0], [1, 0]).Should().BeApproximately(1, 0.0001);
        VectorMath.CosineSimilarity([1, 0], [0, 1]).Should().BeApproximately(0, 0.0001);
        var zero = () => VectorMath.CosineSimilarity([0, 0], [1, 0]);
        zero.Should().Throw<EmbeddingUnavailableException>();
        var mismatch = () => VectorMath.CosineSimilarity([1], [1, 0]);
        mismatch.Should().Throw<EmbeddingUnavailableException>();
    }

    [Fact]
    public async Task VectorIndex_ConcurrentFirstUseBuildsOnce()
    {
        var source = new StubSource([
            new SpecDocumentChunk("issues", "openspec/specs/issues/spec.md", "work item", 0, "one")
        ]);
        var embeddings = new StubEmbeddings(_ => [new float[] { 1, 0 }]);
        var index = new InMemorySpecVectorIndex(source, embeddings, Options([("EmbeddingModel", "embed") ]));

        var results = await Task.WhenAll(index.GetOrBuildAsync(), index.GetOrBuildAsync(), index.GetOrBuildAsync());

        source.Calls.Should().Be(1);
        embeddings.Calls.Should().Be(1);
        results.Should().OnlyContain(item => ReferenceEquals(item, results[0]));
    }

    [Fact]
    public async Task VectorIndex_FailedRebuildPreservesLastSnapshot()
    {
        var source = new StubSource([new SpecDocumentChunk("issues", "source", "issue", 0, "one")]);
        var fail = false;
        var embeddings = new StubEmbeddings(_ => fail
            ? throw new EmbeddingUnavailableException("failed")
            : [new float[] { 1, 0 }]);
        var index = new InMemorySpecVectorIndex(source, embeddings, Options([("EmbeddingModel", "embed")]));
        var first = await index.GetOrBuildAsync();
        fail = true;

        var action = () => index.RebuildAsync();

        await action.Should().ThrowAsync<EmbeddingUnavailableException>();
        index.Snapshot.Should().BeSameAs(first);
        index.Status.Should().Be("Ready");
    }

    [Fact]
    public async Task EmbeddingRetriever_UsesSemanticRankingAndThreshold()
    {
        var entries = new[]
        {
            new SpecVectorEntry(new SpecDocumentChunk("issue-management", "issue.md", "Issue rules", 0, "one"), [1, 0]),
            new SpecVectorEntry(new SpecDocumentChunk("auth", "auth.md", "Login rules", 0, "two"), [0, 1])
        };
        var index = new StubIndex(new SpecIndexSnapshot(entries, "embed", DateTimeOffset.UtcNow));
        var embeddings = new StubEmbeddings(_ => [new float[] { .9f, .1f }]);
        var retriever = new EmbeddingSpecDocumentRetriever(index, embeddings,
            Microsoft.Extensions.Options.Options.Create(new SpecAssistantOptions { MinimumSimilarity = .8, TopK = 3 }));

        var result = await retriever.RetrieveAsync("work item");

        result.Chunks.Should().ContainSingle();
        result.Chunks[0].Capability.Should().Be("issue-management");
    }

    private static IOptions<SpecAssistantOptions> Options(IEnumerable<(string Key, string Value)> values)
    {
        var model = values.ToDictionary(item => item.Key, item => item.Value);
        return Microsoft.Extensions.Options.Options.Create(new SpecAssistantOptions
        { EmbeddingModel = model.GetValueOrDefault("EmbeddingModel", "embed") });
    }

    private sealed class StubSource(IReadOnlyList<SpecDocumentChunk> chunks) : ISpecDocumentSource
    {
        public int Calls { get; private set; }
        public Task<IReadOnlyList<SpecDocumentChunk>> LoadAsync(CancellationToken cancellationToken = default)
        { Calls++; return Task.FromResult(chunks); }
    }

    private sealed class StubEmbeddings(Func<IReadOnlyList<string>, IReadOnlyList<float[]>> create) : IEmbeddingClient
    {
        public int Calls { get; private set; }
        public Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> inputs, CancellationToken cancellationToken = default)
        { Calls++; return Task.FromResult(create(inputs)); }
    }

    private sealed class StubIndex(SpecIndexSnapshot snapshot) : ISpecVectorIndex
    {
        public SpecIndexSnapshot? Snapshot => snapshot;
        public string Status => "Ready";
        public Task<SpecIndexSnapshot> GetOrBuildAsync(CancellationToken cancellationToken = default) => Task.FromResult(snapshot);
        public Task<SpecIndexSnapshot> RebuildAsync(CancellationToken cancellationToken = default) => Task.FromResult(snapshot);
    }
}
