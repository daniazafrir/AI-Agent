using OpenAI.Chat;

namespace Agent.Api.Features.Conversation;

public interface IConversationService
{
    Task<List<ChatMessage>> BuildMessagesAsync(
        Guid conversationId,
        string currentUserMessage,
        string? systemPrompt,
        CancellationToken cancellationToken = default);

    Task SaveConversationAsync(
        Guid conversationId,
        string userMessage,
        string assistantMessage,
        CancellationToken cancellationToken = default);
}