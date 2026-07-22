using OpenAI.Embeddings;

namespace Mcp.Tools.Server.Features.Rag;

public sealed class OpenAiEmbeddingService(
    EmbeddingClient embeddingClient) : IEmbeddingService
{
    public async Task<ReadOnlyMemory<float>> CreateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException(
                "Embedding text cannot be empty.",
                nameof(text));
        }

        OpenAIEmbedding embedding =
            await embeddingClient.GenerateEmbeddingAsync(
                text.Trim(),
                options: null,
                cancellationToken);

        return embedding.ToFloats();
    }
}