
using Agent.Knowledge.Search.Models;

namespace Mcp.Tools.Server.Features.Rag;

public sealed record RagSearchResponse(string Query,RagSearchResult Matches);
