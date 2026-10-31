using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using WEB_Demo.Api.Options;

namespace WEB_Demo.Api.Services;

public interface IAssistantKernelFactory
{
    Kernel Create(AssistantTools tools);
}

public sealed class AssistantKernelFactory(
    IOptions<SpecAssistantOptions> options,
    ILogger<AssistantKernelFactory> logger) : IAssistantKernelFactory
{
    public Kernel Create(AssistantTools tools)
    {
        var settings = options.Value;
        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(settings.Model,
            new Uri(settings.BaseUrl, UriKind.Absolute), settings.ApiKey);
        var kernel = builder.Build();
        kernel.FunctionInvocationFilters.Add(new AssistantFunctionInvocationFilter(
            settings.AssistantMaxToolCalls, settings.AssistantMaxToolResultCharacters, logger));
        kernel.Plugins.AddFromObject(tools, "assistant");
        return kernel;
    }
}
