using WEB_Demo.Api.Models;

namespace WEB_Demo.Api.Services;

public interface ISpecAssistantClient
{
    Task<string> AnswerAsync(string question, IReadOnlyList<SpecDocumentChunk> context, CancellationToken cancellationToken = default);
}

public sealed class SpecAssistantUnavailableException : Exception
{
    public SpecAssistantUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
