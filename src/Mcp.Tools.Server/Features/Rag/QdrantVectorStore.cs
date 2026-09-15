using Agent.Knowledge.Search.Models;
using Google.Protobuf.Collections;
using Mcp.Tools.Server.Features.Rag;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Mcp.Tools.Server.Features.Rag;

public sealed class QdrantVectorStore(
    QdrantClient client,
    IOptions<QdrantOptions> options,
    ILogger<QdrantVectorStore> logger) : IVectorStore
{
    private readonly QdrantOptions _options = options.Value;

    public async Task EnsureCollectionAsync(
        CancellationToken cancellationToken = default)
    {
        if (await client.CollectionExistsAsync(_options.CollectionName, cancellationToken))
        {
            return;
        }

        await client.CreateCollectionAsync(
            _options.CollectionName,
            new VectorParams
            {
                Size = _options.VectorSize,
                Distance = Distance.Cosine
            },
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "Created Qdrant collection {CollectionName}.",
            _options.CollectionName);
    }

    public async Task UpsertAsync(
        IReadOnlyList<VectorChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        if (chunks.Count == 0)
        {
            return;
        }

        await EnsureCollectionAsync(cancellationToken);

        var points = chunks.Select(chunk =>
        {
            var payload = new MapField<string, Value>
            {
                ["documentId"] = chunk.DocumentId.ToString(),
                ["documentName"] = chunk.DocumentName,
                ["chunkIndex"] = chunk.ChunkIndex,
                ["content"] = chunk.Content
            };

            return new PointStruct
            {
                Id = new PointId { Uuid = chunk.Id.ToString() },
                Vectors = chunk.Vector.ToArray(),
                Payload = { payload }
            };
        }).ToList();

        await client.UpsertAsync(
            _options.CollectionName,
            points,
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(
      ReadOnlyMemory<float> queryVector,
      int topK,
      CancellationToken cancellationToken = default)
    {
        await EnsureCollectionAsync(
            cancellationToken);

        var results =
            await client.SearchAsync(
                _options.CollectionName,
                queryVector,
                limit: (ulong)topK,
                payloadSelector: true,
                cancellationToken: cancellationToken);

        return results
            .Select(result =>
            {
                var payload =
                    result.Payload;

                var vectorScore =
                    (double)result.Score;

                return new KnowledgeSearchResult(
                    Guid.Parse(
                        payload["documentId"].StringValue),
                    payload["documentName"].StringValue,
                    (int)payload["chunkIndex"].IntegerValue,
                    payload["content"].StringValue,

                    // At this stage Score is still the
                    // original vector similarity.
                    vectorScore,

                    SearchEngineType.Vector,

                    // Preserve it separately so RRF
                    // cannot destroy the original score.
                    vectorScore);
            })
            .ToList();
    }
    public async Task DeleteDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCollectionAsync(cancellationToken);

        var filter = new Filter
        {
            Must =
            {
                new Condition
                {
                    Field = new FieldCondition
                    {
                        Key = "documentId",
                        Match = new Match { Keyword = documentId.ToString() }
                    }
                }
            }
        };

        await client.DeleteAsync(
            _options.CollectionName,
            filter,
            cancellationToken: cancellationToken);
    }
}
