using System.Diagnostics;
using Agent.Knowledge.Search.Keyword;
using Agent.Knowledge.Search.Models;
using Mcp.Tools.Server.Features.Rag.Ranking;
using Microsoft.Extensions.Logging;

namespace Mcp.Tools.Server.Features.Rag.Hybrid;

public sealed class HybridSearchService(
    IKeywordSearchService keywordSearch,
    IVectorStore vectorStore,
    IEmbeddingService embeddingService,
    RrfRanker ranker,
    ILogger<HybridSearchService> logger)
    : IHybridSearchService
{
    public async Task<HybridSearchResult> SearchAsync(
        string query,
        int topK,
        CancellationToken cancellationToken = default)
    {
        var stopwatch =
            Stopwatch.StartNew();

        var embedding =
            await embeddingService.CreateEmbeddingAsync(
                query,
                cancellationToken);

        var vectorTask =
            vectorStore.SearchAsync(
                embedding,
                topK,
                cancellationToken);

        var keywordTask =
            keywordSearch.SearchAsync(
                query,
                topK,
                cancellationToken);

        await Task.WhenAll(
            vectorTask,
            keywordTask);

        var vectorResults =
            await vectorTask;

        var keywordResults =
            await keywordTask;

        var merged =
            ranker.Merge(
                vectorResults,
                keywordResults,
                topK);

        stopwatch.Stop();

        logger.LogInformation(
            "Vector results: {VectorCount}, Keyword results: {KeywordCount}, Merged results: {MergedCount}",
            vectorResults.Count,
            keywordResults.Count,
            merged.Count);

        return new HybridSearchResult
        {
            Matches = merged,
            VectorResults = vectorResults.Count,
            KeywordResults = keywordResults.Count,
            MergedResults = merged.Count,
            SearchTimeMs = stopwatch.ElapsedMilliseconds
        };
    }
}