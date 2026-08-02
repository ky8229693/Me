using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using WEB_Demo.Api.Models;
using WEB_Demo.Api.Options;

namespace WEB_Demo.Api.Services;

public sealed partial class LexicalSpecDocumentRetriever : ISpecDocumentRetriever
{
    private readonly string _root;
    private readonly SpecAssistantOptions _options;

    public LexicalSpecDocumentRetriever(IHostEnvironment environment, IOptions<SpecAssistantOptions> options)
    {
        _options = options.Value;
        _root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, _options.OpenSpecRoot));
    }

    public async Task<SpecRetrievalResult> RetrieveAsync(string question, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_root))
        {
            return new SpecRetrievalResult([]);
        }

        var terms = Tokenize(question);
        if (terms.Count == 0)
        {
            return new SpecRetrievalResult([]);
        }

        var matches = new List<SpecDocumentChunk>();
        foreach (var file in Directory.EnumerateFiles(_root, "*.md", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fullPath = Path.GetFullPath(file);
            if (!IsUnderRoot(fullPath)) continue;

            var relativePath = Path.GetRelativePath(Path.GetDirectoryName(_root)!, fullPath).Replace('\\', '/');
            var capability = ResolveCapability(relativePath);
            var content = await File.ReadAllTextAsync(fullPath, cancellationToken);

            foreach (var chunk in SplitChunks(content))
            {
                var searchable = $"{capability} {relativePath} {chunk}".ToLowerInvariant();
                var score = terms.Sum(term => searchable.Contains(term, StringComparison.Ordinal) ? 1d : 0d);
                if (score > 0)
                {
                    matches.Add(new SpecDocumentChunk(capability, relativePath, chunk, score));
                }
            }
        }

        var selected = new List<SpecDocumentChunk>();
        var characters = 0;
        foreach (var match in matches.OrderByDescending(item => item.Score).ThenBy(item => item.SourcePath, StringComparer.Ordinal))
        {
            if (selected.Count >= _options.MaxCandidateChunks) break;
            var remaining = _options.MaxContextCharacters - characters;
            if (remaining <= 0) break;
            var content = match.Content.Length <= remaining ? match.Content : match.Content[..remaining];
            selected.Add(match with { Content = content });
            characters += content.Length;
        }

        return new SpecRetrievalResult(selected);
    }

    private bool IsUnderRoot(string path) =>
        path.StartsWith(_root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase);

    private static IReadOnlySet<string> Tokenize(string value) =>
        TokenRegex().Matches(value.ToLowerInvariant())
            .Select(match => match.Value)
            .Where(term => term.Length >= 2)
            .ToHashSet(StringComparer.Ordinal);

    private static IEnumerable<string> SplitChunks(string content) =>
        BlankLineRegex().Split(content)
            .Select(chunk => chunk.Trim())
            .Where(chunk => chunk.Length > 0);

    private static string ResolveCapability(string path)
    {
        var segments = path.Split('/');
        for (var index = 0; index < segments.Length - 1; index++)
        {
            if (segments[index].Equals("specs", StringComparison.OrdinalIgnoreCase))
            {
                return segments[index + 1];
            }
        }

        return Path.GetFileNameWithoutExtension(path);
    }

    [GeneratedRegex(@"[\p{L}\p{N}_-]+", RegexOptions.CultureInvariant)]
    private static partial Regex TokenRegex();

    [GeneratedRegex(@"\r?\n\s*\r?\n", RegexOptions.CultureInvariant)]
    private static partial Regex BlankLineRegex();
}
