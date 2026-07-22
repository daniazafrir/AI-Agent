namespace Agent.Api.Entitites;

public sealed class Conversation
{
    public Guid Id { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public List<ConversationMessage> Messages { get; set; } = [];
}
