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
            var answer = string.IsNullOrWhiteSpace(response.Content) ? "請確認或補充上方資訊。" : response.Content.Trim();
            var message = await conversations.AppendAsync(conversation, AssistantMessageRole.Assistant,
                answer, null, cancellationToken);
            var loaded = await conversations.LoadAsync(conversationId, userId, cancellationToken);
            return new AssistantTurnResponse(message, loaded?.PendingAction, tools.Sources.Distinct().ToArray(),
                loaded?.PendingAction is null ? "completed" : "confirmation_required");
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
