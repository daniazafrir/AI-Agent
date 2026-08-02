using OpenAI.Chat;

namespace Agent.Api.Features.Conversation;

public sealed class ConversationService(
    IConversationStore conversationStore)
    : IConversationService
{
    private const string UserRole = "user";
    private const string AssistantRole = "assistant";

    public async Task<List<ChatMessage>> BuildMessagesAsync(
        Guid conversationId,
        string currentUserMessage,
        string? systemPrompt,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage>();

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            messages.Add(new SystemChatMessage(systemPrompt));
        }

        var history = await conversationStore.GetMessagesAsync(
            conversationId,
            cancellationToken);

        foreach (var storedMessage in history)
        {
            if (string.Equals(storedMessage.Role, UserRole, StringComparison.OrdinalIgnoreCase))
            {
                messages.Add(new UserChatMessage(storedMessage.Content));
            }
            else if (string.Equals(storedMessage.Role, AssistantRole, StringComparison.OrdinalIgnoreCase))
            {
                messages.Add(new AssistantChatMessage(storedMessage.Content));
            }
        }

        messages.Add(new UserChatMessage(currentUserMessage.Trim()));

        return messages;
    }

    public async Task SaveConversationAsync(
        Guid conversationId,
        string userMessage,
        string assistantMessage,
        CancellationToken cancellationToken = default)
    {
        await conversationStore.AddMessageAsync(
            conversationId,
            UserRole,
            userMessage,
            cancellationToken);

        await conversationStore.AddMessageAsync(
            conversationId,
            AssistantRole,
            assistantMessage,
            cancellationToken);
    }
}