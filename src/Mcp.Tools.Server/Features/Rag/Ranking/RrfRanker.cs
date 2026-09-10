using Agent.Knowledge.Search.Models;
using Microsoft.Extensions.Logging;

namespace Mcp.Tools.Server.Features.Rag.Ranking;

public sealed class RrfRanker
{
    private const int K = 60;

    public IReadOnlyList<KnowledgeSearchResult> Merge(
     IReadOnlyList<KnowledgeSearchResult> vectorResults,
     IReadOnlyList<KnowledgeSearchResult> keywordResults,
     int topK)
    {
        Console.WriteLine(
    $"Vector={vectorResults.Count}, Keyword={keywordResults.Count}");

        var scores =
            new Dictionary<(Guid, int), double>();

        var results =
            new Dictionary<(Guid, int), KnowledgeSearchResult>();

        var sources =
            new Dictionary<(Guid, int), HashSet<SearchEngineType>>();

        AddResults(
            vectorResults,
            SearchEngineType.Vector);

        AddResults(
            keywordResults,
            SearchEngineType.Keyword);

        Console.WriteLine("VECTOR");

        foreach (var r in vectorResults)
        {
            Console.WriteLine(
                $"{r.DocumentId} {r.ChunkIndex}");
        }

        Console.WriteLine("KEYWORD");

        foreach (var r in keywordResults)
        {
            Console.WriteLine(
                $"{r.DocumentId} {r.ChunkIndex}");
        }

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

                return new KnowledgeSearchResult(
                    original.DocumentId,
                    original.DocumentName,
                    original.ChunkIndex,
                    original.Content,
                    x.Value,
                    searchEngine
                );
            })
            .ToList();

        void AddResults(
            IReadOnlyList<KnowledgeSearchResult> list,
            SearchEngineType engine)
        {
            for (var rank = 0; rank < list.Count; rank++)
            {
                var result =
                    list[rank];

                var key =
                    (
                        result.DocumentId,
                        result.ChunkIndex
                    );

                var score =
                    1.0 / (K + rank + 1);

                if (!scores.TryAdd(
                        key,
                        score))
                {
                    scores[key] += score;
                }

                results[key] =
                    result;

                if (!sources.TryGetValue(
                        key,
                        out var engines))
                {
                    engines =
                        new HashSet<SearchEngineType>();

                    sources[key] =
                        engines;
                }

                engines.Add(engine);
            }
        }
    }
}