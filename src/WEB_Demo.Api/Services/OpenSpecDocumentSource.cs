using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using WEB_Demo.Api.Models;
using WEB_Demo.Api.Options;

namespace WEB_Demo.Api.Services;

public sealed partial class OpenSpecDocumentSource : ISpecDocumentSource
{
    private readonly string _root;
    private readonly SpecAssistantOptions _options;

    public OpenSpecDocumentSource(IHostEnvironment environment, IOptions<SpecAssistantOptions> options)
    {
        _options = options.Value;
        _root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, _options.OpenSpecRoot));
    }

    public async Task<IReadOnlyList<SpecDocumentChunk>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_root)) return [];
        var chunks = new List<SpecDocumentChunk>();
        foreach (var file in Directory.EnumerateFiles(_root, "*.md", SearchOption.AllDirectories)
                     .OrderBy(GetSourcePriority)
                     .ThenBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fullPath = Path.GetFullPath(file);
            if (!IsUnderRoot(fullPath)) continue;
            var relativePath = Path.GetRelativePath(Path.GetDirectoryName(_root)!, fullPath).Replace('\\', '/');
            var capability = ResolveCapability(relativePath);
            var content = await File.ReadAllTextAsync(fullPath, cancellationToken);
            var index = 0;
            foreach (var section in BlankLineRegex().Split(content).Select(value => value.Trim()).Where(value => value.Length > 0))
            {
                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(section)))[..12].ToLowerInvariant();
                chunks.Add(new SpecDocumentChunk(capability, relativePath, section, 0, $"{relativePath}:{index++}:{hash}"));
                if (chunks.Count >= _options.MaxIndexedChunks) return chunks;
            }
        }
        return chunks;
    }

    private bool IsUnderRoot(string path) => path.StartsWith(
        _root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
        StringComparison.OrdinalIgnoreCase);

    private int GetSourcePriority(string path)
    {
        var relativePath = Path.GetRelativePath(_root, path).Replace('\\', '/');
        if (relativePath.StartsWith("specs/", StringComparison.OrdinalIgnoreCase)) return 0;
        if (relativePath.StartsWith("changes/archive/", StringComparison.OrdinalIgnoreCase)) return 2;
        return 1;
    }

    private static string ResolveCapability(string path)
    {
        var segments = path.Split('/');
        for (var index = 0; index < segments.Length - 1; index++)
            if (segments[index].Equals("specs", StringComparison.OrdinalIgnoreCase)) return segments[index + 1];
        return Path.GetFileNameWithoutExtension(path);
    }

    [GeneratedRegex(@"\r?\n\s*\r?\n", RegexOptions.CultureInvariant)]
    private static partial Regex BlankLineRegex();
}
