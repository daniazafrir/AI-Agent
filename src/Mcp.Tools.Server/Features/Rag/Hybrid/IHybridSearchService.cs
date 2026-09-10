using Agent.Knowledge.Search.Models;

namespace Mcp.Tools.Server.Features.Rag.Hybrid;

public interface IHybridSearchService
{
    Task<HybridSearchResult> SearchAsync(
    string query,
    int topK,
    CancellationToken cancellationToken = default);
}