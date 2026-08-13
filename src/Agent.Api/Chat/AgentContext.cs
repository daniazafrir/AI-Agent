using OpenAI.Chat;

namespace Agent.Api.Chat;

public sealed class AgentContext
{
    public required Guid ConversationId { get; init; }

    public required List<ChatMessage> Messages { get; init; }


    public List<string> UsedTools { get; } = [];

    public int Round { get; set; }
}