using WEB_Demo.Api.Models;

namespace WEB_Demo.Api.Services;

public interface ISpecDocumentRetriever
{
    Task<SpecRetrievalResult> RetrieveAsync(string question, CancellationToken cancellationToken = default);
}
