using Agent.Api.Chat;
using Agent.Api.Chat.Models;
using Agent.Api.Features.Conversation;

public sealed class ChatOrchestrator(
    IConversationService conversationService,
    IAgentRuntime agentRuntime,
    ILogger<ChatOrchestrator> logger)
    : IChatOrchestrator
{
    private const string SystemPrompt =
    """
    You are an AI assistant with access to external tools.

    Answer general knowledge questions from your own knowledge.

    Use search_knowledge ONLY when the user asks about:
    - uploaded documents
    - internal company information
    - HR policies
    - contracts
    - procedures
    - employee handbooks
    - information that is likely stored in the knowledge base

    Do NOT use search_knowledge for:
    - general definitions
    - programming concepts
    - science
    - history
    - mathematics
    - public knowledge

    Examples:

    User: What is MCP?
    -> Answer directly. Do NOT call search_knowledge.

    User: What is Angular?
    -> Answer directly. Do NOT call search_knowledge.

    User: What does the employee handbook say about vacation days?
    -> Call search_knowledge.

    Always answer in the same language as the user's latest message.
    """;

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
                SystemPrompt,
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
            UsedTools = result.UsedTools.ToArray(),
            Sources = result.Sources
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

        yield return new ChatStreamEvent
        {
            Type = "conversation",
            ConversationId = conversationId
        };

        var messages =
            await conversationService.BuildMessagesAsync(
                conversationId,
                request.Message,
                SystemPrompt,
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

            if (streamEvent.Type == "completed")
            {
                logger.LogInformation(
                    "Completed stream event. UsedTools: {UsedToolsCount}, Sources: {SourcesCount}",
                    streamEvent.UsedTools?.Count ?? 0,
                    streamEvent.Sources?.Count ?? 0);
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