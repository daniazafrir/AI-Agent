namespace Mcp.Tools.Server.Features.Rag;

public sealed record DocumentChunk(Guid Id,Guid DocumentId,string DocumentName,int ChunkIndex,string Content,IReadOnlyList<string> SearchTerms);
