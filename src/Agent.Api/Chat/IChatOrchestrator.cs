using Agent.Api.Contracts;

namespace Agent.Api.Chat;

public interface IChatOrchestrator
{
    Task<ChatResponse> ChatAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<ChatStreamEvent> ChatStreamingAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default);
}