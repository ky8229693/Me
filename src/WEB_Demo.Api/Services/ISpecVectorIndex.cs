using WEB_Demo.Api.Models;

namespace WEB_Demo.Api.Services;

public interface ISpecVectorIndex
{
    SpecIndexSnapshot? Snapshot { get; }
    string Status { get; }
    Task<SpecIndexSnapshot> GetOrBuildAsync(CancellationToken cancellationToken = default);
    Task<SpecIndexSnapshot> RebuildAsync(CancellationToken cancellationToken = default);
}
