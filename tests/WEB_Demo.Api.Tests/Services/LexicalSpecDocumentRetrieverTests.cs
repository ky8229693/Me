using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using WEB_Demo.Api.Options;
using WEB_Demo.Api.Services;

namespace WEB_Demo.Api.Tests.Services;

public sealed class LexicalSpecDocumentRetrieverTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"web-demo-specs-{Guid.NewGuid():N}");

    [Fact]
    public async Task RetrieveAsync_SelectsBoundedMarkdownAndReturnsNormalizedMetadata()
    {
        var specDirectory = Path.Combine(_root, "openspec", "specs", "issue-management");
        Directory.CreateDirectory(specDirectory);
        await File.WriteAllTextAsync(Path.Combine(specDirectory, "spec.md"),
            "### Requirement: Issue permission\n\nProject members can create an Issue.");
        await File.WriteAllTextAsync(Path.Combine(_root, "outside.md"), "Issue permission outside root");

        var result = await CreateRetriever(maxChunks: 1).RetrieveAsync("Issue permission");

        result.Chunks.Should().ContainSingle();
        result.Chunks[0].Capability.Should().Be("issue-management");
        result.Chunks[0].SourcePath.Should().Be("openspec/specs/issue-management/spec.md");
        result.Chunks[0].Content.Should().NotContain("outside root");
    }

    [Fact]
    public async Task RetrieveAsync_ReturnsNoMatchesForUnrelatedQuestion()
    {
        var specDirectory = Path.Combine(_root, "openspec", "specs", "projects");
        Directory.CreateDirectory(specDirectory);
        await File.WriteAllTextAsync(Path.Combine(specDirectory, "spec.md"), "Project creation rules");

        var result = await CreateRetriever().RetrieveAsync("quantum banana");

        result.HasMatches.Should().BeFalse();
    }

    private LexicalSpecDocumentRetriever CreateRetriever(int maxChunks = 6) => new(
        new TestHostEnvironment { ContentRootPath = _root },
        Microsoft.Extensions.Options.Options.Create(new SpecAssistantOptions { OpenSpecRoot = "openspec", MaxCandidateChunks = maxChunks }));

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "WEB_Demo.Api.Tests";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
