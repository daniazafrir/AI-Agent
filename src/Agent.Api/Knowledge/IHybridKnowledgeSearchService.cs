using Agent.Knowledge.Search.Models;

namespace Agent.Api.Knowledge;
public interface IHybridKnowledgeSearchService
{
    Task<IReadOnlyList<KnowledgeSearchResult>>
        SearchAsync(
            string query,
            int topK,
            CancellationToken cancellationToken = default);
}
