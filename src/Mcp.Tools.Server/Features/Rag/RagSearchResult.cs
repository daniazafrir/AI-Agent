namespace Mcp.Tools.Server.Features.Rag;

public sealed record RagSearchResult(Guid DocumentId,string DocumentName,int ChunkIndex,string Content,double Score);
