using Agent.Api.Contracts;

namespace Agent.Api.Features.Chat;

public interface IAgentService
{
    Task<ChatResponse> ChatAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default);
}
