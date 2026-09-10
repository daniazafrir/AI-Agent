using Agent.Api.Chat;
using Agent.Api.Chat.Models;
using OpenAI.Chat;
using System.Runtime.CompilerServices;

public sealed class AgentRuntime(
    IAgentLoop loop)
    : IAgentRuntime
{
    public Task<AgentRunResult> RunAsync(
        ICollection<ChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var context =
            CreateContext(messages);

        return loop.RunAsync(
            context,
            cancellationToken);
    }

    public async IAsyncEnumerable<ChatStreamEvent>
        RunStreamingAsync(
            ICollection<ChatMessage> messages,
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var context =
            CreateContext(messages);

        await foreach (
            var streamEvent in
                loop.RunStreamingAsync(
                    context,
                    cancellationToken)
                .WithCancellation(
                    cancellationToken))
        {
            yield return streamEvent;
        }
    }

    private static AgentContext CreateContext(
        ICollection<ChatMessage> messages)
    {
        return new AgentContext
        {
            ConversationId = Guid.Empty,
            Messages = messages.ToList()
        };
    }
}