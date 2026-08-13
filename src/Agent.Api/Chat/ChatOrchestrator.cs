using Agent.Api.Chat;
using Agent.Api.Features.Conversation;

public sealed class ChatOrchestrator(
    IConversationService conversationService,
    IAgentRuntime agentRuntime,
    ILogger<ChatOrchestrator> logger)
    : IChatOrchestrator
{

    public async Task<Agent.Api.Contracts.ChatResponse> ChatAsync(
    Agent.Api.Contracts.ChatRequest request,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException(
                "Message is required.",
                nameof(request));
        }

        var conversationId =
            request.ConversationId
            ?? Guid.NewGuid();

        var messages =
            await conversationService.BuildMessagesAsync(
                conversationId,
                request.Message,
                null,
                cancellationToken);

        var result =
            await agentRuntime.RunAsync(
                messages,
                cancellationToken);

        await conversationService.SaveConversationAsync(
            conversationId,
            request.Message,
            result.AssistantMessage,
            cancellationToken);

        return new Agent.Api.Contracts.ChatResponse
        {
            ConversationId = conversationId,
            Answer = result.AssistantMessage,
            UsedTools = result.UsedTools.ToArray()
        };
    }
   

    public async IAsyncEnumerable<ChatStreamEvent> ChatStreamingAsync(
        Agent.Api.Contracts.ChatRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        var conversationId =
            request.ConversationId
            ?? Guid.NewGuid();

        var messages =
            await conversationService.BuildMessagesAsync(
                conversationId,
                request.Message,
                null,
                cancellationToken);

        var assistantText =
            new System.Text.StringBuilder();

        var usedTools =
            new List<string>();

        await foreach (
            var streamEvent in
                agentRuntime.RunStreamingAsync(
                    messages,
                    cancellationToken))
        {
            if (
                streamEvent.Type == "content" &&
                !string.IsNullOrEmpty(
                    streamEvent.Content))
            {
                assistantText.Append(
                    streamEvent.Content);
            }

            if (
                streamEvent.UsedTools is not null)
            {
                usedTools.AddRange(
                    streamEvent.UsedTools);
            }

            yield return streamEvent;
        }

        var finalAssistantMessage =
            assistantText.ToString();

        if (!string.IsNullOrWhiteSpace(
                finalAssistantMessage))
        {
            await conversationService
                .SaveConversationAsync(
                    conversationId,
                    request.Message,
                    finalAssistantMessage,
                    cancellationToken);
        }
    }

  
}