using WEB_Demo.Api.Models;

namespace WEB_Demo.Api.Services;

public interface ISpecDocumentSource
{
    Task<IReadOnlyList<SpecDocumentChunk>> LoadAsync(CancellationToken cancellationToken = default);
}
