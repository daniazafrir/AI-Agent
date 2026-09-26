namespace Agent.Api.Entitites;

public sealed class ConversationMessage
{
    public long Id { get; set; }

    public Guid ConversationId { get; set; }

    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
    public string? TraceJson { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public Conversation Conversation { get; set; } = null!;
}
