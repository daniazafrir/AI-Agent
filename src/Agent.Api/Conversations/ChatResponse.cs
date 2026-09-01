namespace Agent.Api.Features.Conversation;

public sealed class ChatResponse
{
    public required Guid ConversationId { get; set; }

    public required string Answer { get; set; }

    public IReadOnlyList<string> UsedTools { get; set; } = [];

    public List<KnowledgeSource> Sources { get; init; } = [];
}

public sealed class KnowledgeSource
{
    public Guid DocumentId { get; init; }

    public string DocumentName { get; init; } =
        string.Empty;

    public int ChunkIndex { get; init; }

    public double Score { get; init; }
}