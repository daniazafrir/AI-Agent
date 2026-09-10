using Agent.Knowledge.Search.Models;

namespace Agent.Knowledge.Search.Keyword;

public interface IKeywordSearchService
{
    Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(
        string query,
        int topK,
        CancellationToken cancellationToken = default);
}