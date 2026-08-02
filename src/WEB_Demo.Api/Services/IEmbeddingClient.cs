namespace WEB_Demo.Api.Services;

public interface IEmbeddingClient
{
    Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> inputs, CancellationToken cancellationToken = default);
}

public sealed class EmbeddingUnavailableException : Exception
{
    public EmbeddingUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
}
