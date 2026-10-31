using Microsoft.SemanticKernel;

namespace WEB_Demo.Api.Services;

public sealed class AssistantFunctionInvocationFilter(
    int maximumCalls, int maximumResultCharacters,
    ILogger logger) : IFunctionInvocationFilter
{
    private static readonly string[] BlockedMutationTerms = ["create", "update", "delete", "assign", "archive", "restore"];
    private int calls;

    public async Task OnFunctionInvocationAsync(
        FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        var functionName = context.Function.Name;
        if (BlockedMutationTerms.Any(term => functionName.StartsWith(term + "_", StringComparison.OrdinalIgnoreCase)) &&
            !functionName.StartsWith("prepare_", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Direct mutation tools are not available to the assistant.");
        if (Interlocked.Increment(ref calls) > maximumCalls)
            throw new InvalidOperationException("Assistant tool-call limit exceeded.");

        logger.LogInformation("Assistant tool {Plugin}.{Function} started", context.Function.PluginName, functionName);
        await next(context);
        var text = context.Result?.ToString();
        if (text?.Length > maximumResultCharacters)
        {
            context.Result = new FunctionResult(context.Function,
                text[..maximumResultCharacters] + "…[truncated]");
        }
        logger.LogInformation("Assistant tool {Plugin}.{Function} completed", context.Function.PluginName, functionName);
    }
}
