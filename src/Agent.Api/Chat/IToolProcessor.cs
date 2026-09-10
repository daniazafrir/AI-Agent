using Agent.Api.Chat.Models;

namespace Agent.Api.Chat;

public interface IToolProcessor
{
    Task ProcessAsync(
        ChatCompletionResult completion,
        AgentContext context,
        CancellationToken cancellationToken);

    IAsyncEnumerable<ChatStreamEvent> ProcessStreamingAsync(
        ChatCompletionResult completion,
        AgentContext context,
        CancellationToken cancellationToken = default);
}