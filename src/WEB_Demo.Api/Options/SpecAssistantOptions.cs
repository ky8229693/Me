namespace WEB_Demo.Api.Options;

public sealed class SpecAssistantOptions
{
    public const string SectionName = "SpecAssistant";

    public string OpenSpecRoot { get; init; } = "../../openspec";
    public string BaseUrl { get; init; } = "https://api.openai.com/v1/";
    public string Model { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string RetrievalMode { get; init; } = "Embedding";
    public string VectorStore { get; init; } = "PostgreSql";
    public string EmbeddingBaseUrl { get; init; } = "http://localhost:11434/v1/";
    public string EmbeddingModel { get; init; } = string.Empty;
    public int EmbeddingDimension { get; init; } = 1024;
    public string EmbeddingApiKey { get; init; } = string.Empty;
    public int EmbeddingBatchSize { get; init; } = 16;
    public int MaxIndexedChunks { get; init; } = 500;
    public int TopK { get; init; } = 6;
    public double MinimumSimilarity { get; init; } = 0.35;
    public int TimeoutSeconds { get; init; } = 30;
    public int MaxQuestionLength { get; init; } = 1000;
    public int MaxCandidateChunks { get; init; } = 6;
    public int MaxContextCharacters { get; init; } = 12000;
}
