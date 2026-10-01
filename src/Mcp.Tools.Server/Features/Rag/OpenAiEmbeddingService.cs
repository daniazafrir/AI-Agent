using OpenAI.Embeddings;

namespace Mcp.Tools.Server.Features.Rag;

public sealed class OpenAiEmbeddingService(
    EmbeddingClient embeddingClient) : IEmbeddingService
{
    // Record the model bound to this client, rather than re-reading mutable configuration.
#pragma warning disable OPENAI001
    public string? ModelName => embeddingClient.Model;
#pragma warning restore OPENAI001
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
