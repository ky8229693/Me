namespace WEB_Demo.Api.Options;

public sealed class SpecAssistantOptions
{
    public const string SectionName = "SpecAssistant";

    public string OpenSpecRoot { get; init; } = "../../openspec";
    public string BaseUrl { get; init; } = "https://api.openai.com/v1/";
    public string Model { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 30;
    public int MaxQuestionLength { get; init; } = 1000;
    public int MaxCandidateChunks { get; init; } = 6;
    public int MaxContextCharacters { get; init; } = 12000;
}
