namespace Mcp.Tools.Server.Features.Rag;

public sealed class QdrantOptions
{
    public const string SectionName = "Qdrant";
    public string Host { get; init; } = "localhost";
    public int GrpcPort { get; init; } = 6334;
    public string CollectionName { get; init; } = "agent_documents";
    public ulong VectorSize { get; init; } = 1536;
}
