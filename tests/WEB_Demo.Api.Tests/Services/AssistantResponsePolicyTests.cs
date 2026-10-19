using WEB_Demo.Api.Models;
using WEB_Demo.Api.Services;
using FluentAssertions;

namespace WEB_Demo.Api.Tests.Services;

public sealed class AssistantResponsePolicyTests
{
    [Theory]
    [InlineData("建立測試議題", AssistantActionIntent.CreateIssue)]
    [InlineData("新增一個 Bug", AssistantActionIntent.CreateIssue)]
    [InlineData("Create an issue", AssistantActionIntent.CreateIssue)]
    [InlineData("建立新專案", AssistantActionIntent.CreateProject)]
    public void Detect_FindsExplicitWriteIntent(string message, AssistantActionIntent expected) =>
        AssistantActionIntentDetector.Detect(message).Should().Be(expected);

    [Fact]
    public void Resolve_DoesNotTrustModelProseWhenRequiredToolWasNotCalled()
    {
        var result = AssistantResponsePolicy.Resolve(AssistantActionIntent.CreateIssue, null, null,
            "請稍候，我會告訴您結果。");

        result.State.Should().Be("tool_required");
        result.Answer.Should().Contain("沒有執行任何操作").And.NotContain("請稍候");
    }

    [Fact]
    public void Resolve_ReturnsStructuredMissingFieldPrompt()
    {
        var outcome = new AssistantPreparationOutcome("missing_fields", null, ["projectId", "title"], null);

        var result = AssistantResponsePolicy.Resolve(AssistantActionIntent.CreateIssue, outcome, null,
            "正在處理");

        result.State.Should().Be("missing_fields");
        result.Answer.Should().Contain("專案").And.Contain("議題標題").And.NotContain("正在處理");
    }

    [Fact]
    public void Resolve_ReturnsConfirmationOnlyWithPersistedPendingAction()
    {
        var action = new AssistantPendingActionInfo(Guid.NewGuid(), "CreateIssue", "Pending",
            System.Text.Json.JsonSerializer.SerializeToElement(new { title = "測試議題" }),
            DateTimeOffset.UtcNow.AddMinutes(15), null);
        var outcome = new AssistantPreparationOutcome("confirmation_required", null, [], action);

        var result = AssistantResponsePolicy.Resolve(AssistantActionIntent.CreateIssue, outcome, action,
            "請稍候");

        result.State.Should().Be("confirmation_required");
        result.Answer.Should().Contain("議題已準備完成").And.NotContain("請稍候");
    }
}
