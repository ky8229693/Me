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
    double Score);

public sealed record SpecRetrievalResult(IReadOnlyList<SpecDocumentChunk> Chunks)
{
    public bool HasMatches => Chunks.Count > 0;
}
