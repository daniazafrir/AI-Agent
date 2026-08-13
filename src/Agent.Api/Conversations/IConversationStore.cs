using Agent.Api.Entitites;

namespace Agent.Api.Features.Conversation;

public interface IConversationStore
{
    Task<IReadOnlyList<ConversationSummary>> GetConversationsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversationMessage>> GetMessagesAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);

    Task AddMessageAsync(
        Guid conversationId,
        string role,
        string content,
        CancellationToken cancellationToken = default);

    Task DeleteConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);
}