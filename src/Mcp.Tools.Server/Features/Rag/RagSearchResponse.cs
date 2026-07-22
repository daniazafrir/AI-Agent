
namespace Mcp.Tools.Server.Features.Rag;

public sealed record RagSearchResponse(string Query,IReadOnlyList<RagSearchResult> Matches);
