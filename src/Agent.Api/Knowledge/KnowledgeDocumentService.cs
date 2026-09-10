using Agent.Api.Entitites;
using Agent.Api.Features.Knowledge.Chunking;
using Agent.Api.Features.Knowledge.Extraction;
using Agent.Api.Knowledge;
using Agent.Api.Mcp;
using Agent.Knowledge.Entitites;
using Agent.Knowledge.Repositories;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Agent.Api.Features.Knowledge;

public sealed class KnowledgeDocumentService(
    IMcpToolClient mcpToolClient,
    IKnowledgeDocumentRepository repository,
    IKnowledgeChunkRepository chunkRepository,
    DocumentTextExtractorFactory extractorFactory,
    IChunkingService chunkingService,
    ILogger<KnowledgeDocumentService> logger)
    : IKnowledgeDocumentService
{
    public async Task<DocumentUploadResponse> UploadAsync(
    string fileName,
    Stream stream,
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

        ArgumentNullException.ThrowIfNull(stream);

        var extension =
            Path.GetExtension(
                safeFileName);

        var extractor =
            extractorFactory.Get(
                extension);

        var content =
            await extractor.ExtractAsync(
                stream,
                cancellationToken);

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException(
                "Document content cannot be empty.",
                nameof(stream));
        }

        var normalizedContent =
            content.Trim();

        var contentHash =
            ComputeHash(
                normalizedContent);

        var existing =
            await repository.FindByHashAsync(
                contentHash,
                cancellationToken);

        if (existing is not null)
        {
            logger.LogInformation(
                "Document {FileName} already exists as {DocumentId}.",
                safeFileName,
                existing.Id);

            return new DocumentUploadResponse
            {
                Success = true,
                AlreadyExists = true,
                DocumentId = existing.Id,
                FileName = existing.FileName,
                ChunkCount = existing.ChunkCount,
                Message = "Document already exists."
            };
        }

        /*
         * Agent.Api is now responsible for chunking.
         */
        var chunks =
            chunkingService.Split(
                normalizedContent);

        if (chunks.Count == 0)
        {
            throw new InvalidOperationException(
                "The document did not produce any chunks.");
        }

        /*
         * Generate the document ID here.
         * Later this same ID will also be sent to MCP.
         */
        var documentId =
            Guid.NewGuid();

        /*
         * IMPORTANT:
         * During this intermediate step MCP still receives
         * the complete content, not the chunks.
         *
         * We will change this in the next step.
         */
        var arguments =
            new Dictionary<string, object?>
            {
                ["documentId"] = documentId,
                ["fileName"] = safeFileName,
                ["chunks"] = chunks
            };

        var rawResult =
            await mcpToolClient.CallToolAsync(
                "index_document",
                arguments,
                cancellationToken);

        var indexResult =
            ParseIndexResult(
                rawResult);

        if (!indexResult.Success)
        {
            throw new InvalidOperationException(
                $"MCP failed to index document: {indexResult.Message}");
        }


        var sizeBytes =
            Encoding.UTF8.GetByteCount(
                normalizedContent);

        var document =
            new KnowledgeDocument
            {
                Id = documentId,
                FileName = safeFileName,
                SizeBytes = sizeBytes,
                ChunkCount = chunks.Count,
                ContentHash = contentHash,
                CreatedAtUtc = DateTime.UtcNow
            };

        /*
         * Save document first because KnowledgeChunks
         * have a foreign key to it.
         */
        await repository.AddAsync(
            document,
            cancellationToken);

        var knowledgeChunks =
            chunks
                .Select(
                    (chunkContent, index) =>
                        new KnowledgeChunk
                        {
                            Id = Guid.NewGuid(),
                            DocumentId = documentId,
                            ChunkIndex = index,
                            Content = chunkContent,
                            CreatedAtUtc = DateTime.UtcNow
                        })
                .ToList();

        await chunkRepository.AddRangeAsync(
            knowledgeChunks,
            cancellationToken);

        logger.LogInformation(
            "Knowledge document {FileName} ({DocumentId}) persisted with {ChunkCount} chunks.",
            safeFileName,
            documentId,
            knowledgeChunks.Count);

        return new DocumentUploadResponse
        {
            Success = true,
            AlreadyExists = false,
            DocumentId = document.Id,
            FileName = document.FileName,
            ChunkCount = document.ChunkCount,
            Message = "Document indexed successfully."
        };
    }

    public async Task<IReadOnlyList<KnowledgeDocument>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return await repository.ListAsync(
            cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var documents =
            await repository.ListAsync(
                cancellationToken);

        var existing =
          await  repository.GetByIdAsync(documentId);

        if (existing is null)
        {
            return false;
        }

        var arguments =
            new Dictionary<string, object?>
            {
                ["documentId"] = documentId
            };

        var rawResult =
            await mcpToolClient.CallToolAsync(
                "delete_vectors",
                arguments,
                cancellationToken);

        if (!ParseDeleteResult(rawResult))
        {
            logger.LogWarning(
                "MCP did not delete vectors for document {DocumentId}.",
                documentId);

            return false;
        }
        logger.LogInformation(
    "Deleting document {FileName} ({DocumentId}).",
    existing.FileName,
    documentId);

        await chunkRepository.DeleteByDocumentAsync(
    documentId,
    cancellationToken);


        var deleted =
            await repository.DeleteAsync(
                documentId,
                cancellationToken);

        if (!deleted)
        {
            logger.LogWarning(
                "Document {DocumentId} was deleted from Qdrant but not found in PostgreSQL.",
                documentId);
        }
        logger.LogInformation(
    "Document {FileName} ({DocumentId}) deleted successfully.",
    existing.FileName,
    documentId);
        return deleted;
    }


    private static string ComputeHash(
        string content)
    {
        var bytes =
            Encoding.UTF8.GetBytes(
                content.Trim());

        var hash =
            SHA256.HashData(
                bytes);

        return Convert.ToHexString(
            hash);
    }

    private static McpIndexResult ParseIndexResult(
        string rawResult)
    {
        try
        {
            return JsonSerializer.Deserialize<McpIndexResult>(
                       rawResult,
                       JsonOptions)
                   ?? throw new InvalidOperationException(
                       "MCP returned an empty index result.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                "MCP returned an invalid index_document response.",
                exception);
        }
    }

    private static bool ParseDeleteResult(
        string rawResult)
    {
        if (bool.TryParse(
                rawResult,
                out var directResult))
        {
            return directResult;
        }

        try
        {
            using var document =
                JsonDocument.Parse(
                    rawResult);

            if (
                document.RootElement.ValueKind ==
                JsonValueKind.True)
            {
                return true;
            }

            if (
                document.RootElement.ValueKind ==
                JsonValueKind.False)
            {
                return false;
            }

            if (
                document.RootElement.TryGetProperty(
                    "success",
                    out var success) &&
                success.ValueKind is
                    JsonValueKind.True or
                    JsonValueKind.False)
            {
                return success.GetBoolean();
            }

            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public async Task<KnowledgeChunkResponse?> GetChunkAsync(
    Guid documentId,
    int chunkIndex,
    CancellationToken cancellationToken = default)
    {
        if (chunkIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(chunkIndex));
        }

        var chunk =
            await chunkRepository.GetAsync(
                documentId,
                chunkIndex,
                cancellationToken);

        if (chunk is null)
        {
            return null;
        }

        var document =
            await repository.GetByIdAsync(
                documentId,
                cancellationToken);

        if (document is null)
        {
            return null;
        }

        return new KnowledgeChunkResponse
        {
            DocumentId = documentId,
            DocumentName = document.FileName,
            ChunkIndex = chunk.ChunkIndex,
            Content = chunk.Content
        };
    }

    public async Task<bool> ReindexAsync(
    Guid documentId,
    CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var document =
            await repository.GetByIdAsync(
                documentId,
                cancellationToken);

        if (document is null)
        {
            return false;
        }

        var chunks =
            await chunkRepository.ListByDocumentAsync(
                documentId,
                cancellationToken);

        if (chunks.Count == 0)
        {
            throw new InvalidOperationException(
                $"Document {documentId} has no persisted chunks.");
        }

        logger.LogInformation(
            "Re-indexing document {FileName} ({DocumentId}) with {ChunkCount} chunks.",
            document.FileName,
            documentId,
            chunks.Count);

        var deleteArguments =
            new Dictionary<string, object?>
            {
                ["documentId"] = documentId
            };

        var deleteResult =
            await mcpToolClient.CallToolAsync(
                "delete_vectors",
                deleteArguments,
                cancellationToken);

        if (!ParseDeleteResult(deleteResult))
        {
            throw new InvalidOperationException(
                $"Could not remove existing vectors for document {documentId}.");
        }

        var orderedChunks =
            chunks
                .OrderBy(x => x.ChunkIndex)
                .Select(x => x.Content)
                .ToList();

        var indexArguments =
            new Dictionary<string, object?>
            {
                ["documentId"] = documentId,
                ["fileName"] = document.FileName,
                ["chunks"] = orderedChunks
            };

        var rawResult =
            await mcpToolClient.CallToolAsync(
                "index_document",
                indexArguments,
                cancellationToken);

        var indexResult =
            ParseIndexResult(rawResult);

        if (!indexResult.Success)
        {
            throw new InvalidOperationException(
                $"MCP failed to re-index document: {indexResult.Message}");
        }

        logger.LogInformation(
            "Document {FileName} ({DocumentId}) re-indexed successfully with {ChunkCount} chunks.",
            document.FileName,
            documentId,
            indexResult.ChunkCount);

        return true;
    }


    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNameCaseInsensitive = true
        };

    private sealed class McpIndexResult
    {
        public bool Success { get; init; }

        public Guid DocumentId { get; init; }

        public string FileName { get; init; } =
            string.Empty;

        public int ChunkCount { get; init; }

        public string Message { get; init; } =
            string.Empty;
    }
}