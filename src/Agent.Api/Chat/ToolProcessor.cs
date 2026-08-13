using Agent.Api.Tools;
using OpenAI.Chat;

namespace Agent.Api.Chat;

public sealed class ToolProcessor(
    IToolExecutor toolExecutor,
    ILogger<ToolProcessor> logger)
    : IToolProcessor
{
    public async Task ProcessAsync(
        ChatCompletionResult completion,
        AgentContext context,
        CancellationToken cancellationToken)
    {
        context.Messages.Add(
            new AssistantChatMessage(
                completion.RawCompletion));

        foreach (var tool in completion.ToolCalls)
        {
            logger.LogInformation(
                "Executing tool {Tool}.",
                tool.Name);

            context.UsedTools.Add(
                tool.Name);

            var result =
                await toolExecutor.ExecuteAsync(
                    tool.Name,
                    tool.Arguments,
                    cancellationToken);

            context.Messages.Add(
                new ToolChatMessage(
                    tool.Id,
                    result));
        }
    }
}