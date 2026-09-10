namespace Agent.Api.Features.Conversation;
public sealed class KnowledgeSource
{
    public Guid DocumentId { get; init; }

    public string DocumentName { get; init; } =
        string.Empty;

    public int ChunkIndex { get; init; }

    public double Score { get; init; }
}