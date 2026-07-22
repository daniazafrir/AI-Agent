namespace Agent.Api.Features.Conversation;

public sealed class ChatResponse
{
    public required Guid ConversationId { get; set; }

    public required string Answer { get; set; }

    public IReadOnlyList<string> UsedTools { get; set; } = [];
}