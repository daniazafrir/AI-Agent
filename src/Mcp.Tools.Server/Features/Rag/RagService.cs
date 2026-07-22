using Mcp.Tools.Server.Features.Rag;

namespace Mcp.Tools.Server.Features.Rag;

public sealed class RagService(
    ITextExtractionService textExtractionService,
    IChunkingService chunkingService,
    IEmbeddingService embeddingService,
    IVectorStore vectorStore,
    IRagDocumentStore documentStore,
    ILogger<RagService> logger) : IRagService
{
    public async Task<DocumentInfo> UploadAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var text = await textExtractionService.ExtractAsync(file, cancellationToken);
        var chunkTexts = chunkingService.Split(text);

        var documentId = Guid.NewGuid();
        var documentName = Path.GetFileName(file.FileName);
        var vectorChunks = new List<VectorChunk>(chunkTexts.Count);

        for (var index = 0; index < chunkTexts.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var content = chunkTexts[index];
            var vector = await embeddingService.CreateEmbeddingAsync(
                content,
                cancellationToken);

            vectorChunks.Add(new VectorChunk(
                Guid.NewGuid(),
                documentId,
                documentName,
                index,
                content,
                vector));
        }

        await vectorStore.UpsertAsync(vectorChunks, cancellationToken);

        var document = new DocumentInfo(
            documentId,
            documentName,
            file.Length,
            vectorChunks.Count,
            DateTime.UtcNow);

        await documentStore.SaveAsync(document, cancellationToken);

        logger.LogInformation(
            "Indexed document {DocumentName} with {ChunkCount} vector chunks.",
            documentName,
            vectorChunks.Count);

        return document;
    }


    public async Task<DocumentIndexResult> IndexDocumentAsync(
        string fileName,
        string content,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var safeFileName = Path.GetFileName(fileName?.Trim());

        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            throw new ArgumentException(
                "File name is required.",
                nameof(fileName));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException(
                "Document content cannot be empty.",
                nameof(content));
        }

        var chunkTexts = chunkingService.Split(content);

        if (chunkTexts.Count == 0)
        {
            throw new InvalidOperationException(
                "The document did not produce any indexable chunks.");
        }

        var documentId = Guid.NewGuid();
        var vectorChunks = new List<VectorChunk>(chunkTexts.Count);

        for (var index = 0; index < chunkTexts.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var chunkContent = chunkTexts[index];
            var vector = await embeddingService.CreateEmbeddingAsync(
                chunkContent,
                cancellationToken);

            vectorChunks.Add(new VectorChunk(
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

        var sizeBytes = System.Text.Encoding.UTF8.GetByteCount(content);
        var document = new DocumentInfo(
            documentId,
            safeFileName,
            sizeBytes,
            vectorChunks.Count,
            DateTime.UtcNow);

        await documentStore.SaveAsync(
            document,
            cancellationToken);

        logger.LogInformation(
            "Indexed document {DocumentName} ({DocumentId}) with {ChunkCount} chunks through MCP.",
            safeFileName,
            documentId,
            vectorChunks.Count);

        return new DocumentIndexResult
        {
            Success = true,
            DocumentId = documentId,
            FileName = safeFileName,
            ChunkCount = vectorChunks.Count,
            Message = "Document indexed successfully."
        };
    }

    public async Task<IReadOnlyList<RagSearchResult>> SearchAsync(
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

        var queryVector = await embeddingService.CreateEmbeddingAsync(
            query,
            cancellationToken);

        logger.LogInformation(
            "Searching knowledge base for '{Query}'. Vector size: {VectorSize}, TopK: {TopK}",
            query,
            queryVector.Length,
            topK);

        var results = await vectorStore.SearchAsync(
            queryVector,
            topK,
            cancellationToken);

        logger.LogInformation(
            "Knowledge search returned {ResultCount} results.",
            results.Count);

        foreach (var result in results)
        {
            logger.LogInformation(
                "Match: {DocumentName}, score: {Score}, content: {Content}",
                result.DocumentName,
                result.Score,
                result.Content);
        }

        return results;
    }
    public Task<IReadOnlyList<DocumentInfo>> ListDocumentsAsync(
        CancellationToken cancellationToken = default)
        => documentStore.ListAsync(cancellationToken);

    public async Task<bool> DeleteDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var deleted = await documentStore.DeleteAsync(documentId, cancellationToken);

        if (!deleted)
        {
            return false;
        }

        await vectorStore.DeleteDocumentAsync(documentId, cancellationToken);
        return true;
    }

}
