using Agent.Api.Configuration;
using Agent.Api.Contracts;
using Agent.Api.Features.Conversation;
using Microsoft.Extensions.Options;

namespace Agent.Api.Chat;

public sealed class ChatOrchestrator(
    IConversationService conversationService,
    IAgentRuntime agentRuntime,
    IOptions<AgentOptions> agentOptions,
    ILogger<ChatOrchestrator> logger)
    : IChatOrchestrator
{
    private readonly AgentOptions _agentOptions = agentOptions.Value;

    public async Task<Contracts.ChatResponse> ChatAsync(
        Contracts.ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateRequest(request);

        var conversationId = ResolveConversationId(
            request.ConversationId);

        logger.LogInformation(
            "Starting chat orchestration for conversation {ConversationId}.",
            conversationId);

        var messages =
            await conversationService.BuildMessagesAsync(
                conversationId,
                request.Message,
                _agentOptions.SystemPrompt,
                cancellationToken);

        var agentResult =
            await agentRuntime.RunAsync(
                messages,
                cancellationToken);

        await conversationService.SaveConversationAsync(
            conversationId,
            request.Message,
            agentResult.AssistantMessage,
            cancellationToken);

        logger.LogInformation(
            "Completed chat orchestration for conversation {ConversationId}.",
            conversationId);

        return new Contracts.ChatResponse
        {
            ConversationId = conversationId,
            Answer = agentResult.AssistantMessage,
            UsedTools = agentResult.UsedTools
        };
    }

    private static void ValidateRequest(Contracts.ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException(
                "Message is required.",
                nameof(request));
        }
    }

    private static Guid ResolveConversationId(
        Guid? requestedConversationId)
    {
        return requestedConversationId is { } conversationId
               && conversationId != Guid.Empty
            ? conversationId
            : Guid.NewGuid();
    }
}