namespace Agent.Api.Features.Conversation;

public interface IConversationService
{
    Task<ChatResponse> ChatAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default);
}
