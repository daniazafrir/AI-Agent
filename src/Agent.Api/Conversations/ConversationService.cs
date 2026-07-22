namespace Agent.Api.Features.Conversation;

public sealed class ConversationService(
    IConversationStore conversationStore)
    : IConversationService
{
    public async Task<ChatResponse> ChatAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var conversationId =
            request.ConversationId ?? Guid.NewGuid();

        await conversationStore.AddMessageAsync(
            conversationId,
            "user",
            request.Message,
            cancellationToken);

        // בשלב זה זו תשובת Placeholder.
        // בשלב הבא נחבר ל-OpenAI ול-Tool Calling.
        const string answer = "Agent placeholder response";

        await conversationStore.AddMessageAsync(
            conversationId,
            "assistant",
            answer,
            cancellationToken);

        return new ChatResponse
        {
            ConversationId = conversationId,
            Answer = answer,
            UsedTools = []
        };
    }
}