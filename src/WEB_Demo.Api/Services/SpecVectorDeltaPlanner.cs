using System.Security.Cryptography;
using System.Text;
using WEB_Demo.Api.Data;
using WEB_Demo.Api.Models;

namespace WEB_Demo.Api.Services;

public sealed record SpecVectorDelta(
    IReadOnlyList<SpecDocumentChunk> Changed,
    IReadOnlyList<SpecVectorDocument> Stale,
    int Unchanged);

public static class SpecVectorDeltaPlanner
{
    public static SpecVectorDelta Plan(
        IReadOnlyList<SpecDocumentChunk> chunks,
        IReadOnlyDictionary<string, SpecVectorDocument> existing,
        int dimension)
    {
        var desiredIds = chunks.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        var changed = chunks.Where(chunk => !existing.TryGetValue(chunk.Id, out var row) ||
            row.ContentHash != Hash(chunk.Content) || row.Dimension != dimension).ToArray();
        var stale = existing.Values.Where(item => !desiredIds.Contains(item.ChunkId)).ToArray();
        return new SpecVectorDelta(changed, stale, chunks.Count - changed.Length);
    }

    public static string Hash(string content) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
}
