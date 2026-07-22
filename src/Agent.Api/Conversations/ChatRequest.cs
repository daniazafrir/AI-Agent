namespace Agent.Api.Features.Conversation;

public sealed class ChatRequest
{
    public Guid? ConversationId { get; set; }

    public required string Message { get; set; }
}