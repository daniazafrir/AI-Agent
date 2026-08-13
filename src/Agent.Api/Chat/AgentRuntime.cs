using Agent.Api.Chat;
using OpenAI.Chat;

public sealed class AgentRuntime(
    IAgentLoop loop)
    : IAgentRuntime
{
    public Task<AgentRunResult> RunAsync(
        ICollection<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        var context =
            CreateContext(messages);

        return loop.RunAsync(
            context,
            cancellationToken);
    }

    private static AgentContext CreateContext(
        ICollection<ChatMessage> messages)
    {
        return new AgentContext
        {
            ConversationId = Guid.Empty,
            Messages = messages.ToList(),
        };
    }

    public async IAsyncEnumerable<ChatStreamEvent> RunStreamingAsync(
    ICollection<ChatMessage> messages,
    [System.Runtime.CompilerServices.EnumeratorCancellation]
    CancellationToken cancellationToken = default)
    {
        var result =
            await RunAsync(
                messages,
                cancellationToken);

        yield return new ChatStreamEvent
        {
            Type = "content",
            Content = result.AssistantMessage
        };

        yield return new ChatStreamEvent
        {
            Type = "completed",
            UsedTools = result.UsedTools
        };
    }
}