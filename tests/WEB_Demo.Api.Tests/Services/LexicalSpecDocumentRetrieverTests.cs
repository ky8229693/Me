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

        var retriever = CreateRetriever(maxChunks: 1);
        var result = await retriever.RetrieveAsync("Issue permission");

        result.Chunks.Should().ContainSingle();
        result.Chunks[0].Capability.Should().Be("issue-management");
        result.Chunks[0].SourcePath.Should().Be("openspec/specs/issue-management/spec.md");
        result.Chunks[0].Content.Should().NotContain("outside root");
        result.Chunks[0].Id.Should().NotBeNullOrWhiteSpace();
        var repeated = await retriever.RetrieveAsync("Issue permission");
        repeated.Chunks[0].Id.Should().Be(result.Chunks[0].Id);
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

    [Fact]
    public async Task RetrieveAsync_ExpandsNonTechnicalProjectStageQuestionToIssueWorkflowTerms()
    {
        var specDirectory = Path.Combine(_root, "openspec", "specs", "issue-workflow");
        Directory.CreateDirectory(specDirectory);
        await File.WriteAllTextAsync(Path.Combine(specDirectory, "spec.md"),
            "Issue 狀態流程包含 Draft、Approved、InProgress、QA 與 Done。");

        var result = await CreateRetriever().RetrieveAsync("專案關卡有哪些？");

        result.Chunks.Should().NotBeEmpty();
        result.Chunks[0].Capability.Should().Be("issue-workflow");
    }

    [Fact]
    public async Task DocumentSource_PrioritizesMainSpecificationsBeforeArchivedChangesWhenBounded()
    {
        var mainSpecDirectory = Path.Combine(_root, "openspec", "specs", "account-management");
        var archiveDirectory = Path.Combine(_root, "openspec", "changes", "archive", "old-change");
        Directory.CreateDirectory(mainSpecDirectory);
        Directory.CreateDirectory(archiveDirectory);
        await File.WriteAllTextAsync(Path.Combine(mainSpecDirectory, "spec.md"), "使用者帳號管理規格");
        await File.WriteAllTextAsync(Path.Combine(archiveDirectory, "proposal.md"), "封存規格");
        var options = Microsoft.Extensions.Options.Options.Create(
            new SpecAssistantOptions { OpenSpecRoot = "openspec", MaxIndexedChunks = 1 });
        var source = new OpenSpecDocumentSource(new TestHostEnvironment { ContentRootPath = _root }, options);

        var chunks = await source.LoadAsync();

        chunks.Should().ContainSingle();
        chunks[0].SourcePath.Should().Be("openspec/specs/account-management/spec.md");
    }

    private LexicalSpecDocumentRetriever CreateRetriever(int maxChunks = 6)
    {
        var options = Microsoft.Extensions.Options.Options.Create(
            new SpecAssistantOptions { OpenSpecRoot = "openspec", MaxCandidateChunks = maxChunks });
        var source = new OpenSpecDocumentSource(new TestHostEnvironment { ContentRootPath = _root }, options);
        return new LexicalSpecDocumentRetriever(source, options);
    }

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
