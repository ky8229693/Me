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
        endpoints.MapGet("/api/spec-assistant/index", async (
            ISpecVectorIndex index, IPersistentSpecVectorStore store, IOptions<SpecAssistantOptions> options,
            CancellationToken cancellationToken) =>
            Results.Ok(ApiResponse<SpecIndexStatusResponse>.Ok(
                await CreateStatusAsync(index, store, options.Value, cancellationToken))))
            .WithTags("Specification Assistant").WithName("GetSpecificationIndexStatus").RequireAuthorization();
        endpoints.MapPost("/api/spec-assistant/index/rebuild", async (
            ISpecVectorIndex index, IPersistentSpecVectorStore store, IOptions<SpecAssistantOptions> options,
            CancellationToken cancellationToken) =>
        {
            if (options.Value.RetrievalMode.Equals("Lexical", StringComparison.OrdinalIgnoreCase))
                return Results.Ok(ApiResponse<SpecIndexStatusResponse>.Ok(
                    await CreateStatusAsync(index, store, options.Value, cancellationToken)));
            try
            {
                if (options.Value.VectorStore.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase))
                {
                    var sync = await store.SyncAsync(cancellationToken);
                    var status = await CreateStatusAsync(index, store, options.Value, cancellationToken, sync);
                    return Results.Ok(ApiResponse<SpecIndexStatusResponse>.Ok(status));
                }
                await index.RebuildAsync(cancellationToken);
                return Results.Ok(ApiResponse<SpecIndexStatusResponse>.Ok(
                    await CreateStatusAsync(index, store, options.Value, cancellationToken)));
            }
            catch (EmbeddingUnavailableException)
            {
                return Results.Json(ApiResponse<object>.Fail(
                    "Specification embedding index is temporarily unavailable or not configured.",
                    "SPEC_EMBEDDING_UNAVAILABLE"), statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }).WithTags("Specification Assistant").WithName("RebuildSpecificationIndex").RequireAuthorization();
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
        catch (Exception exception) when (exception is SpecAssistantUnavailableException or EmbeddingUnavailableException)
        {
            return Results.Json(ApiResponse<object>.Fail(
                "Specification assistant is temporarily unavailable or not configured.",
                "SPEC_ASSISTANT_UNAVAILABLE"), statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    private static async Task<SpecIndexStatusResponse> CreateStatusAsync(
        ISpecVectorIndex index, IPersistentSpecVectorStore store, SpecAssistantOptions options,
        CancellationToken cancellationToken, SpecVectorSyncResult? sync = null)
    {
        var lexical = options.RetrievalMode.Equals("Lexical", StringComparison.OrdinalIgnoreCase);
        var persistent = !lexical && options.VectorStore.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase);
        var persisted = persistent ? await store.GetStatusAsync(cancellationToken) : default;
        return new SpecIndexStatusResponse(
            lexical ? "Lexical" : options.RetrievalMode.Equals("Hybrid", StringComparison.OrdinalIgnoreCase) ? "Hybrid" : "Embedding",
            lexical ? "NotApplicable" : persistent ? persisted.Count > 0 ? "Ready" : "Unbuilt" : index.Status,
            lexical ? null : string.IsNullOrWhiteSpace(options.EmbeddingModel) ? null : options.EmbeddingModel,
            persistent ? persisted.Count : index.Snapshot?.Entries.Count ?? 0,
            persistent ? persisted.SyncedAt : index.Snapshot?.BuiltAt,
            persistent ? "PostgreSql" : "Memory",
            sync?.Inserted ?? 0, sync?.Updated ?? 0, sync?.Deleted ?? 0, sync?.Unchanged ?? 0);
    }
}
