using Agent.Knowledge.Search.Models;

namespace Mcp.Tools.Server.Features.Rag.Ranking;

public sealed class RrfRanker
{
    private const int K = 60;

    public IReadOnlyList<KnowledgeSearchResult> Merge(
        IReadOnlyList<KnowledgeSearchResult> vectorResults,
        IReadOnlyList<KnowledgeSearchResult> keywordResults,
        int topK)
    {
        var scores =
            new Dictionary<(Guid DocumentId, int ChunkIndex), double>();

        var results =
            new Dictionary<
                (Guid DocumentId, int ChunkIndex),
                KnowledgeSearchResult>();

        var sources =
            new Dictionary<
                (Guid DocumentId, int ChunkIndex),
                HashSet<SearchEngineType>>();

        var vectorScores =
            new Dictionary<
                (Guid DocumentId, int ChunkIndex),
                double>();

        AddResults(
            vectorResults,
            SearchEngineType.Vector);

        AddResults(
            keywordResults,
            SearchEngineType.Keyword);

        return scores
            .OrderByDescending(x => x.Value)
            .Take(topK)
            .Select(x =>
            {
                var original =
                    results[x.Key];

                var engines =
                    sources[x.Key];

                var searchEngine =
                    engines.Count > 1
                        ? SearchEngineType.Hybrid
                        : engines.Single();

                vectorScores.TryGetValue(
                    x.Key,
                    out var vectorScore);

                var hasVectorScore =
                    vectorScores.ContainsKey(
                        x.Key);

                return new KnowledgeSearchResult(
                    original.DocumentId,
                    original.DocumentName,
                    original.ChunkIndex,
                    original.Content,

                    // Final RRF score
                    x.Value,

                    searchEngine,

                    // Preserve original Qdrant similarity
                    hasVectorScore
                        ? vectorScore
                        : null);
            })
            .ToList();

        void AddResults(
            IReadOnlyList<KnowledgeSearchResult> list,
            SearchEngineType engine)
        {
            for (
                var rank = 0;
                rank < list.Count;
                rank++)
            {
                var result =
                    list[rank];

                var key =
                    (
                        result.DocumentId,
                        result.ChunkIndex
                    );

                var rrfScore =
                    1.0 /
                    (K + rank + 1);

                if (!scores.TryAdd(
                        key,
                        rrfScore))
                {
                    scores[key] +=
                        rrfScore;
                }

                //
                // Keep one copy of the result.
                //
                if (!results.ContainsKey(key))
                {
                    results[key] =
                        result;
                }

                //
                // Preserve original vector similarity.
                //
                if (engine ==
                    SearchEngineType.Vector)
                {
                    var originalVectorScore =
                        result.VectorScore ??
                        result.Score;

                    vectorScores[key] =
                        originalVectorScore;
                }

                if (!sources.TryGetValue(
                        key,
                        out var engines))
                {
                    engines =
                        new HashSet<SearchEngineType>();

                    sources[key] =
                        engines;
                }

                engines.Add(
                    engine);
            }
        }
    }
}