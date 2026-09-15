using Agent.Knowledge.Search.Keyword;
using Agent.Knowledge.Search.Models;
using Mcp.Tools.Server.Features.Rag.Ranking;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Mcp.Tools.Server.Features.Rag.Hybrid;

public sealed class HybridSearchService(
    IKeywordSearchService keywordSearch,
    IVectorStore vectorStore,
    IEmbeddingService embeddingService,
    RrfRanker ranker,
    IOptions<RagOptions> ragOptions,
    ILogger<HybridSearchService> logger)
    : IHybridSearchService
{
    public async Task<HybridSearchResult> SearchAsync(
        string query,
        int topK,
        CancellationToken cancellationToken = default)
    {
        var totalStopwatch =
            Stopwatch.StartNew();

        //
        // 1. Create embedding
        //
        var embeddingStopwatch =
            Stopwatch.StartNew();

        var embedding =
            await embeddingService.CreateEmbeddingAsync(
                query,
                cancellationToken);

        embeddingStopwatch.Stop();

        //
        // 2. Retrieve more candidates than final topK
        //
        var candidateCount =
            Math.Max(topK * 4, 20);

        //
        // 3. Run vector + keyword search in parallel
        //
        var vectorTask =
            MeasureVectorSearchAsync(
                embedding,
                candidateCount,
                cancellationToken);

        var keywordTask =
            MeasureKeywordSearchAsync(
                query,
                candidateCount,
                cancellationToken);

        await Task.WhenAll(
            vectorTask,
            keywordTask);

        var vector =
            await vectorTask;

        var keyword =
            await keywordTask;

        var rawVectorResults =
            vector.Results;

        var keywordResults =
            keyword.Results;

        //
        // 4. Log raw vector results
        //
        foreach (var result in rawVectorResults)
        {
            logger.LogInformation(
                "Raw vector result. Document={Document}, Chunk={Chunk}, VectorScore={VectorScore}",
                result.DocumentName,
                result.ChunkIndex,
                result.VectorScore);
        }

        //
        // 5. Filter vector results by configured cosine similarity
        //
        var minimumVectorScore =
            ragOptions.Value.MinimumVectorScore;

        var relevantVectorResults =
            rawVectorResults
                .Where(x =>
                    x.VectorScore.HasValue &&
                    x.VectorScore.Value >= minimumVectorScore)
                .ToList();

        logger.LogInformation(
            """
            Vector relevance filtering completed.
            Query: {Query}
            RawVectorResults: {RawVectorCount}
            RelevantVectorResults: {RelevantVectorCount}
            MinimumVectorScore: {MinimumVectorScore}
            """,
            query,
            rawVectorResults.Count,
            relevantVectorResults.Count,
            minimumVectorScore);

        //
        // 6. RRF ranking
        //
        var rankingStopwatch =
            Stopwatch.StartNew();

        var merged =
            ranker.Merge(
                relevantVectorResults,
                keywordResults,
                topK);

        rankingStopwatch.Stop();

        totalStopwatch.Stop();

        //
        // 7. Final diagnostics
        //
        logger.LogInformation(
            """
            Hybrid search completed.
            Query: {Query}
            CandidateCount: {CandidateCount}
            RawVectorResults: {RawVectorCount}
            RelevantVectorResults: {RelevantVectorCount}
            KeywordResults: {KeywordCount}
            MergedResults: {MergedCount}
            MinimumVectorScore: {MinimumVectorScore}
            EmbeddingTime: {EmbeddingTimeMs}ms
            VectorSearchTime: {VectorSearchTimeMs}ms
            KeywordSearchTime: {KeywordSearchTimeMs}ms
            RankingTime: {RankingTimeMs}ms
            TotalTime: {TotalTimeMs}ms
            """,
            query,
            candidateCount,
            rawVectorResults.Count,
            relevantVectorResults.Count,
            keywordResults.Count,
            merged.Count,
            minimumVectorScore,
            embeddingStopwatch.ElapsedMilliseconds,
            vector.TimeMs,
            keyword.TimeMs,
            rankingStopwatch.ElapsedMilliseconds,
            totalStopwatch.ElapsedMilliseconds);

        //
        // 8. Return result
        //
        return new HybridSearchResult
        {
            Matches =
                merged,

            RawVectorResults =
                rawVectorResults.Count,

            RelevantVectorResults =
                relevantVectorResults.Count,

            VectorResults =
                relevantVectorResults.Count,

            KeywordResults =
                keywordResults.Count,

            MergedResults =
                merged.Count,

            MinimumVectorScore =
                minimumVectorScore,

            SearchTimeMs =
                totalStopwatch.ElapsedMilliseconds
        };
    }

    private async Task<(
        IReadOnlyList<KnowledgeSearchResult> Results,
        long TimeMs)>
        MeasureVectorSearchAsync(
            ReadOnlyMemory<float> embedding,
            int candidateCount,
            CancellationToken cancellationToken)
    {
        var stopwatch =
            Stopwatch.StartNew();

        var results =
            await vectorStore.SearchAsync(
                embedding,
                candidateCount,
                cancellationToken);

        stopwatch.Stop();

        return (
            results,
            stopwatch.ElapsedMilliseconds);
    }

    private async Task<(
        IReadOnlyList<KnowledgeSearchResult> Results,
        long TimeMs)>
        MeasureKeywordSearchAsync(
            string query,
            int candidateCount,
            CancellationToken cancellationToken)
    {
        var stopwatch =
            Stopwatch.StartNew();

        var results =
            await keywordSearch.SearchAsync(
                query,
                candidateCount,
                cancellationToken);

        stopwatch.Stop();

        return (
            results,
            stopwatch.ElapsedMilliseconds);
    }
}