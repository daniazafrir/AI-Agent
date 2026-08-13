using Agent.Api.Configuration;
using Agent.Api.Mcp;
using Agent.Api.OpenAI;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Agent.Api.Chat;

public sealed class AgentLoop(
    IChatCompletionService completionService,
    IMcpToolRegistry toolRegistry,
    IToolProcessor toolProcessor,
    IOptions<OpenAiOptions> options)
    : IAgentLoop
{
    private readonly OpenAiOptions _options = options.Value;

    public async Task<AgentRunResult> RunAsync(
        AgentContext context,
        CancellationToken cancellationToken)
    {
        await toolRegistry.InitializeAsync(
            cancellationToken);

        var chatOptions =
            BuildOptions();

        while (context.Round < _options.MaxToolRounds)
        {
            context.Round++;

            var completion =
                await completionService.CompleteAsync(
                    context.Messages,
                    chatOptions,
                    cancellationToken);

            if (completion.FinishReason ==
                ChatFinishReason.ToolCalls)
            {
                if (completion.ToolCalls.Count == 0)
                {
                    throw new InvalidOperationException(
                        "OpenAI returned ToolCalls finish reason without any tool calls.");
                }

                await toolProcessor.ProcessAsync(
                    completion,
                    context,
                    cancellationToken);

                continue;
            }

            if (completion.FinishReason ==
                ChatFinishReason.Stop)
            {
                var assistantMessage =
                    string.IsNullOrWhiteSpace(
                        completion.AssistantMessage)
                        ? "The agent completed the request but returned no textual response."
                        : completion.AssistantMessage;

                return new AgentRunResult
                {
                    AssistantMessage =
                        assistantMessage,

                    UsedTools =
                        context.UsedTools
                            .Distinct(
                                StringComparer.OrdinalIgnoreCase)
                            .ToList()
                };
            }

            throw new InvalidOperationException(
                $"Unsupported finish reason: {completion.FinishReason}.");
        }

        throw new InvalidOperationException(
            $"Agent exceeded the maximum number of {_options.MaxToolRounds} rounds.");
    }

    private ChatCompletionOptions BuildOptions()
    {
        var options =
            new ChatCompletionOptions();

        foreach (var tool in toolRegistry.Definitions)
        {
            options.Tools.Add(tool);
        }

        return options;
    }
}