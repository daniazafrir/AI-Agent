namespace Agent.Api.Contracts;

public sealed class ChatRequest
{
    public Guid? ConversationId { get; init; }

    public string Message { get; init; } = string.Empty;
}
