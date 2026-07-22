namespace Mcp.Tools.Server.Features.Rag;

public interface IChunkingService
{
    IReadOnlyList<string> Split(string text,int chunkSize=800,int overlap=150);
}
