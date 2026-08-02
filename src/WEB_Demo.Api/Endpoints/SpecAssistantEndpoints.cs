using WEB_Demo.Api.Models;
using WEB_Demo.Api.Options;
using WEB_Demo.Api.Services;
using Microsoft.Extensions.Options;

namespace WEB_Demo.Api.Endpoints;

public static class SpecAssistantEndpoints
{
    private const string NotDefinedMessage = "目前規格沒有定義與此問題相關的資訊。";

    public static IEndpointRouteBuilder MapSpecAssistantEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/spec-assistant/query", QueryAsync)
            .WithTags("Specification Assistant")
            .WithName("QuerySpecificationAssistant")
            .RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> QueryAsync(
        SpecAssistantQueryRequest request,
        ISpecDocumentRetriever retriever,
        ISpecAssistantClient assistant,
        IOptions<SpecAssistantOptions> options,
        CancellationToken cancellationToken)
    {
        var question = request.Question?.Trim();
        if (string.IsNullOrWhiteSpace(question) || question.Length > options.Value.MaxQuestionLength)
        {
            return Results.BadRequest(ApiResponse<object>.Fail(
                $"Question is required and must not exceed {options.Value.MaxQuestionLength} characters.",
                "SPEC_ASSISTANT_INVALID_QUESTION"));
        }

        var result = await retriever.RetrieveAsync(question, cancellationToken);
        if (!result.HasMatches)
        {
            return Results.Ok(ApiResponse<SpecAssistantQueryResponse>.Ok(
                new SpecAssistantQueryResponse(NotDefinedMessage, [], [])));
        }

        try
        {
            var answer = await assistant.AnswerAsync(question, result.Chunks, cancellationToken);
            var relatedSpecs = result.Chunks.Select(item => item.Capability).Distinct(StringComparer.Ordinal).ToArray();
            var sources = result.Chunks.Select(item => item.SourcePath).Distinct(StringComparer.Ordinal).ToArray();
            return Results.Ok(ApiResponse<SpecAssistantQueryResponse>.Ok(
                new SpecAssistantQueryResponse(answer, relatedSpecs, sources)));
        }
        catch (SpecAssistantUnavailableException)
        {
            return Results.Json(ApiResponse<object>.Fail(
                "Specification assistant is temporarily unavailable or not configured.",
                "SPEC_ASSISTANT_UNAVAILABLE"), statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
