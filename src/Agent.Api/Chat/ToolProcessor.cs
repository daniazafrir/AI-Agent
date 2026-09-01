using Agent.Api.Tools;
using OpenAI.Chat;
using System.Runtime.CompilerServices;

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
        await foreach (
            var _ in ProcessStreamingAsync(
                completion,
                context,
                cancellationToken))
        {
            // Ignore streaming events in non-streaming mode.
        }
    }

    public async IAsyncEnumerable<ChatStreamEvent>
        ProcessStreamingAsync(
            ChatCompletionResult completion,
            AgentContext context,
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(completion);
        ArgumentNullException.ThrowIfNull(context);

        context.Messages.Add(
            new AssistantChatMessage(
                completion.RawCompletion));
        
        logger.LogInformation(
    "Conversation after tool:");

        foreach (var message in context.Messages)
        {
            logger.LogInformation(
                "{Type}: {Content}",
                message.GetType().Name,
                message.ToString());
        }

        foreach (var tool in completion.ToolCalls)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(tool.Id))
            {
                throw new InvalidOperationException(
                    "Tool call ID cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(tool.Name))
            {
                throw new InvalidOperationException(
                    "Tool call name cannot be empty.");
            }

            logger.LogInformation(
                "Executing tool {Tool} with arguments: {Arguments}",
                tool.Name,
                tool.Arguments.ToString());

            yield return new ChatStreamEvent
            {
                Type = "tool-started",
                ToolName = tool.Name
            };

            var result =
                await toolExecutor.ExecuteAsync(
                    tool.Name,
                    tool.Arguments,
                    cancellationToken);

            logger.LogInformation(
                "Tool {Tool} result: {Result}",
                tool.Name,
                result);

            if (!context.UsedTools.Contains(
                    tool.Name,
                    StringComparer.OrdinalIgnoreCase))
            {
                context.UsedTools.Add(tool.Name);
            }

            context.Messages.Add(
                new ToolChatMessage(
                    tool.Id,
                    result.Content));

            yield return new ChatStreamEvent
            {
                Type = "tool-completed",
                ToolName = tool.Name
            };
        }

        yield break;
    }
}