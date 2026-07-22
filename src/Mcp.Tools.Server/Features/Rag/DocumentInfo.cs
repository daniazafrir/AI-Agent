namespace Mcp.Tools.Server.Features.Rag;

public sealed record DocumentInfo(Guid Id,string FileName,long SizeBytes,int ChunkCount,DateTime CreatedAtUtc);
