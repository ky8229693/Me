using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using WEB_Demo.Api.Data;
using WEB_Demo.Api.Models;
using WEB_Demo.Api.Options;
using Microsoft.AspNetCore.Identity;

namespace WEB_Demo.Api.Services;

public sealed class AssistantOrchestrationService(
    AssistantConversationService conversations,
    ISpecDocumentRetriever specifications,
    ProjectAdministrationService projects,
    UserManager<IdentityUser> users,
    IPermissionResolver permissions,
    IOptions<SpecAssistantOptions> options,
    ILogger<AssistantOrchestrationService> logger)
{
    private const string SystemPrompt = """
        你是專案管理小助手，使用繁體中文簡潔回答。
        你可查詢規格與使用者可存取的專案，也可準備建立專案或議題。
        必須透過工具取得受保護資料。資料不足時只詢問缺少欄位。
        寫入工具只會建立預覽；未取得使用者明確確認前，絕不可聲稱已完成建立。
        一次處理一個待確認動作。工具結果若為 ambiguous，請使用者選擇，不可自行猜測。
        """;

    public async Task<AssistantTurnResponse?> SendAsync(
        Guid conversationId, string userId, string? text, CancellationToken cancellationToken)
    {
        var content = text?.Trim();
        if (string.IsNullOrWhiteSpace(content) || content.Length > options.Value.MaxQuestionLength)
            throw new AssistantMessageValidationException(options.Value.MaxQuestionLength);
        var conversation = await conversations.FindOwnedAsync(conversationId, userId, cancellationToken);
        if (conversation is null) return null;
        await conversations.AppendAsync(conversation, AssistantMessageRole.User, content, null, cancellationToken);

        if (string.IsNullOrWhiteSpace(options.Value.Model))
            throw new SpecAssistantUnavailableException("Assistant model is not configured.");

        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(options.Value.Model,
            new Uri(options.Value.BaseUrl, UriKind.Absolute), options.Value.ApiKey);
        var kernel = builder.Build();
        kernel.FunctionInvocationFilters.Add(new AssistantFunctionInvocationFilter(
            options.Value.AssistantMaxToolCalls, options.Value.AssistantMaxToolResultCharacters, logger));
        var tools = new AssistantTools(userId, conversation, conversations, specifications, projects, users, permissions);
        kernel.Plugins.AddFromObject(tools, "assistant");

        var history = new ChatHistory(SystemPrompt);
        foreach (var item in await conversations.GetModelHistoryAsync(conversationId, cancellationToken))
        {
            if (item.Role == AssistantMessageRole.User) history.AddUserMessage(item.Content);
            else if (item.Role == AssistantMessageRole.Assistant) history.AddAssistantMessage(item.Content);
        }
        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            MaxTokens = 1200,
            Temperature = 0.2
        };
        try
        {
            var service = kernel.GetRequiredService<IChatCompletionService>();
            var response = await service.GetChatMessageContentAsync(history, settings, kernel, cancellationToken);
            var actionIntent = AssistantActionIntentDetector.Detect(content);
            if (actionIntent is not null && tools.PreparationOutcome is null)
            {
                logger.LogWarning("Assistant did not call the required preparation tool for {Intent}; retrying with required function",
                    actionIntent);
                history.AddSystemMessage("使用者明確要求寫入操作。你上一個回應沒有執行工具。現在必須呼叫指定的 prepare 工具；不可回覆正在處理、請稍候或稍後完成。");
                var functionName = actionIntent == AssistantActionIntent.CreateIssue
                    ? "prepare_issue_creation" : "prepare_project_creation";
                var requiredSettings = new OpenAIPromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Required(
                        [kernel.Plugins["assistant"][functionName]]),
                    MaxTokens = 1200,
                    Temperature = 0
                };
                response = await service.GetChatMessageContentAsync(
                    history, requiredSettings, kernel, cancellationToken);
            }

            var loaded = await conversations.LoadAsync(conversationId, userId, cancellationToken);
            var (answer, state) = AssistantResponsePolicy.Resolve(actionIntent, tools.PreparationOutcome,
                loaded?.PendingAction, response.Content);
            var message = await conversations.AppendAsync(conversation, AssistantMessageRole.Assistant,
                answer, null, cancellationToken);
            return new AssistantTurnResponse(message, loaded?.PendingAction, tools.Sources.Distinct().ToArray(),
                state);
        }
        catch (AssistantPendingActionConflictException)
        {
            var answer = "目前已有待確認動作，請先確認或取消後再繼續。";
            var message = await conversations.AppendAsync(conversation, AssistantMessageRole.Assistant,
                answer, null, cancellationToken);
            var loaded = await conversations.LoadAsync(conversationId, userId, cancellationToken);
            return new AssistantTurnResponse(message, loaded?.PendingAction, [], "pending_action_conflict");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Assistant orchestration failed for conversation {ConversationId}", conversationId);
            throw new SpecAssistantUnavailableException("Assistant orchestration failed.", exception);
        }
    }

}

public sealed class AssistantMessageValidationException(int maximum) :
    Exception($"Message is required and must not exceed {maximum} characters.");

public enum AssistantActionIntent { CreateProject, CreateIssue }

public static class AssistantActionIntentDetector
{
    private static readonly string[] CreateTerms = ["建立", "新增", "創建", "create", "add"];
    private static readonly string[] IssueTerms = ["議題", "issue", "bug", "錯誤單", "工作項目"];
    private static readonly string[] ProjectTerms = ["專案", "project"];

    public static AssistantActionIntent? Detect(string text)
    {
        if (!CreateTerms.Any(term => text.Contains(term, StringComparison.OrdinalIgnoreCase))) return null;
        if (IssueTerms.Any(term => text.Contains(term, StringComparison.OrdinalIgnoreCase)))
            return AssistantActionIntent.CreateIssue;
        return ProjectTerms.Any(term => text.Contains(term, StringComparison.OrdinalIgnoreCase))
            ? AssistantActionIntent.CreateProject : null;
    }
}

public static class AssistantResponsePolicy
{
    public static (string Answer, string State) Resolve(
        AssistantActionIntent? intent, AssistantPreparationOutcome? outcome,
        AssistantPendingActionInfo? pendingAction, string? modelContent)
    {
        if (pendingAction is not null && outcome?.State == "confirmation_required")
            return (intent == AssistantActionIntent.CreateIssue
                ? "議題已準備完成，請檢查預覽內容後選擇確認執行或取消。"
                : "專案已準備完成，請檢查預覽內容後選擇確認執行或取消。",
                "confirmation_required");
        if (outcome?.State == "missing_fields")
            return ($"請補充以下資訊：{string.Join("、", outcome.MissingFields.Select(FieldLabel))}。", "missing_fields");
        if (outcome is not null)
            return (outcome.Message ?? "無法準備這項操作，請檢查輸入後再試一次。", outcome.State);
        if (intent is not null)
            return ("模型未能呼叫必要的準備工具，因此這次沒有執行任何操作。請再試一次。", "tool_required");
        return (string.IsNullOrWhiteSpace(modelContent) ? "請補充您希望我協助的內容。" : modelContent.Trim(), "completed");
    }

    private static string FieldLabel(string field) => field switch
    {
        "projectId" => "專案",
        "title" => "議題標題",
        "name" => "專案名稱",
        _ => field
    };
}
