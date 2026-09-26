using OpenAI.Chat;

namespace Agent.Api.Features.Conversation;

public sealed class ConversationService(
    IConversationStore conversationStore)
    : IConversationService
{
    private const string UserRole = "user";
    public Task SaveAssistantTraceAsync(Guid conversationId, string message, Agent.Api.Chat.Models.ConversationTrace trace, CancellationToken cancellationToken = default)
        => conversationStore.AddMessageWithTraceAsync(conversationId, AssistantRole, message, trace.ToJson(), cancellationToken);
    private const string AssistantRole = "assistant";

    public Task SaveUserMessageAsync(Guid conversationId, string message, CancellationToken cancellationToken = default)
        => conversationStore.AddMessageAsync(conversationId, UserRole, message, cancellationToken);

    public Task SaveAssistantMessageAsync(Guid conversationId, string message, CancellationToken cancellationToken = default)
        => conversationStore.AddMessageAsync(conversationId, AssistantRole, message, cancellationToken);

    public async Task<List<ChatMessage>> BuildMessagesAsync(
    Guid conversationId,
    string currentUserMessage,
    string? systemPrompt,
    CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(currentUserMessage))
        {
            throw new ArgumentException(
                "Current user message cannot be empty.",
                nameof(currentUserMessage));
        }

        var messages =
            new List<ChatMessage>();

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            messages.Add(
                new SystemChatMessage(
                    systemPrompt.Trim()));
        }

        var history =
            await conversationStore.GetMessagesAsync(
                conversationId,
                cancellationToken);

        foreach (var storedMessage in history)
        {
            if (string.Equals(
                    storedMessage.Role,
                    UserRole,
                    StringComparison.OrdinalIgnoreCase))
            {
                messages.Add(
                    new UserChatMessage(
                        storedMessage.Content));
            }
            else if (string.Equals(
                         storedMessage.Role,
                         AssistantRole,
                         StringComparison.OrdinalIgnoreCase))
            {
                messages.Add(
                    new AssistantChatMessage(
                        storedMessage.Content));
            }
        }

        messages.Add(
            new UserChatMessage(
                currentUserMessage.Trim()));

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
