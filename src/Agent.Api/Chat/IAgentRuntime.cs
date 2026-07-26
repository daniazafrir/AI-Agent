using OpenAI.Chat;

namespace Agent.Api.Chat;

public interface IAgentRuntime
{
    Task<AgentRunResult> RunAsync(
        ICollection<ChatMessage> messages,
        CancellationToken cancellationToken = default);
}