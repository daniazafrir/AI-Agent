using Agent.Knowledge.Search.Models;
using Mcp.Tools.Server.Features.Rag.Hybrid;
using System.Security.Cryptography;
using System.Text;

namespace Mcp.Tools.Server.Features.Rag;

public sealed class RagService(
    ITextExtractionService textExtractionService,
    IEmbeddingService embeddingService,
    IVectorStore vectorStore,
    IHybridSearchService hybridSearch,
    ILogger<RagService> logger) : IRagService
{

    public async Task<DocumentIndexResult> IndexDocumentAsync(
    Guid documentId,
    string fileName,
    IReadOnlyList<string> chunks,
    CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var safeFileName =
            Path.GetFileName(
                fileName?.Trim());

        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            throw new ArgumentException(
                "File name is required.",
                nameof(fileName));
        }


        if (chunks is null || chunks.Count == 0)
        {
            throw new ArgumentException(
                "At least one chunk is required.",
                nameof(chunks));
        }

        var vectorChunks =
            new List<VectorChunk>(
                chunks.Count);

        for (
            var index = 0;
            index < chunks.Count;
            index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var chunkContent =
                chunks[index];

            if (string.IsNullOrWhiteSpace(chunkContent))
            {
                continue;
            }

            var vector =
                await embeddingService.CreateEmbeddingAsync(
                    chunkContent,
                    cancellationToken);

            vectorChunks.Add(
                new VectorChunk(
                    Guid.NewGuid(),
                    documentId,
                    safeFileName,
                    index,
                    chunkContent,
                    vector));
        }

        await vectorStore.UpsertAsync(
            vectorChunks,
            cancellationToken);

        logger.LogInformation(
            "Indexed document {DocumentName} ({DocumentId}) with {ChunkCount} chunks.",
            safeFileName,
            documentId,
            vectorChunks.Count);

        return new DocumentIndexResult
        {
            Success = true,
            DocumentId = documentId,
            ChunkCount = vectorChunks.Count,
            Message = "Document indexed successfully."
        };
    }
    public async Task<RagSearchResult> SearchAsync(
    string query,
    int topK = 5,
    CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException(
                "Search query cannot be empty.",
                nameof(query));
        }

        if (topK is < 1 or > 20)
        {
            throw new ArgumentOutOfRangeException(
                nameof(topK),
                "TopK must be between 1 and 20.");
        }

        query =
            query.Trim();

        var hybridResult =
            await hybridSearch.SearchAsync(
                query,
                topK,
                cancellationToken);

        var uniqueResults =
            hybridResult
                .Matches
                .GroupBy(
                    x => new
                    {
                        x.DocumentId,
                        x.ChunkIndex
                    })
                .Select(group =>
                    group
                        .OrderByDescending(
                            x => x.Score)
                        .First())
                .OrderByDescending(
                    x => x.Score)
                .Take(topK)
                .ToList();

        logger.LogInformation(
            "Knowledge search returned {ResultCount} unique results. Vector={VectorCount}, Keyword={KeywordCount}, Merged={MergedCount}, Time={SearchTimeMs}ms.",
            uniqueResults.Count,
            hybridResult.VectorResults,
            hybridResult.KeywordResults,
            hybridResult.MergedResults,
            hybridResult.SearchTimeMs);

        foreach (var result in uniqueResults)
        {
            logger.LogInformation(
                "Match: {DocumentName}, chunk: {ChunkIndex}, score: {Score}, engine: {SearchEngine}",
                result.DocumentName,
                result.ChunkIndex,
                result.Score,
                result.SearchEngine);
        }

        return new RagSearchResult
        {
            Matches =
                uniqueResults,

            VectorResults =
                hybridResult.VectorResults,

            KeywordResults =
                hybridResult.KeywordResults,

            MergedResults =
                uniqueResults.Count,

            SearchTimeMs =
                hybridResult.SearchTimeMs
        };
    }


    public async Task<bool> DeleteVectorsAsync(
    Guid documentId,
    CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await vectorStore.DeleteDocumentAsync(
            documentId,
            cancellationToken);

        logger.LogInformation(
            "Deleted vectors for document {DocumentId}.",
            documentId);

        return true;
    }

 
    private static string ComputeContentHash(
        string content)
    {
        var normalized =
            content.Trim();

        var bytes =
            Encoding.UTF8.GetBytes(
                normalized);

        var hash =
            SHA256.HashData(
                bytes);

        return Convert.ToHexString(
            hash);
    }
}