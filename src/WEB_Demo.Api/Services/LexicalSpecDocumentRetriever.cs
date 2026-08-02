using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using WEB_Demo.Api.Models;
using WEB_Demo.Api.Options;

namespace WEB_Demo.Api.Services;

public sealed partial class LexicalSpecDocumentRetriever : ISpecDocumentRetriever
{
    private readonly ISpecDocumentSource _source;
    private readonly SpecAssistantOptions _options;
    public LexicalSpecDocumentRetriever(ISpecDocumentSource source, IOptions<SpecAssistantOptions> options)
    { _source = source; _options = options.Value; }

    public async Task<SpecRetrievalResult> RetrieveAsync(string question, CancellationToken cancellationToken = default)
    {
        var terms = TokenRegex().Matches(question.ToLowerInvariant()).Select(match => match.Value)
            .Where(term => term.Length >= 2).ToHashSet(StringComparer.Ordinal);
        foreach (var term in ExpandBusinessTerms(question)) terms.Add(term);
        if (terms.Count == 0) return new SpecRetrievalResult([]);
        var matches = (await _source.LoadAsync(cancellationToken)).Select(chunk =>
        {
            var searchable = $"{chunk.Capability} {chunk.SourcePath} {chunk.Content}".ToLowerInvariant();
            return chunk with { Score = terms.Sum(term => searchable.Contains(term, StringComparison.Ordinal) ? 1d : 0d) };
        }).Where(chunk => chunk.Score > 0).OrderByDescending(chunk => chunk.Score)
          .ThenBy(chunk => chunk.SourcePath, StringComparer.Ordinal).Take(_options.MaxCandidateChunks).ToArray();
        return new SpecRetrievalResult(matches);
    }

    private static IEnumerable<string> ExpandBusinessTerms(string question)
    {
        if (question.Contains("專案關卡", StringComparison.OrdinalIgnoreCase) ||
            question.Contains("工作關卡", StringComparison.OrdinalIgnoreCase))
            return ["issue", "工作項目", "狀態", "工作流程", "workflow", "status"];

        if (question.Contains("使用者管理", StringComparison.OrdinalIgnoreCase))
            return ["使用者", "帳號", "account", "啟用", "停用"];

        return [];
    }

    [GeneratedRegex(@"[\p{L}\p{N}_-]+", RegexOptions.CultureInvariant)]
    private static partial Regex TokenRegex();
}
