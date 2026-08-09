using Agent.Api.Configuration;
using Agent.Api.Contracts;
using Agent.Api.Features.Conversation;
using Microsoft.Extensions.Options;
using Serilog.Context;
using System.Diagnostics;

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
        var stopwatch = Stopwatch.StartNew();

        try
        {
            ArgumentNullException.ThrowIfNull(request);

            ValidateRequest(request);

            var conversationId = ResolveConversationId(
                request.ConversationId);

            using (LogContext.PushProperty(
                       "ConversationId",
                       conversationId))
            {
                logger.LogInformation(
                    "Starting chat orchestration.");
            }

                var messages =
                await conversationService.BuildMessagesAsync(
                    conversationId,
                    request.Message,
                    _agentOptions.SystemPrompt,
                    cancellationToken);

            var agentRunResult =
                await agentRuntime.RunAsync(
                    messages,
                    cancellationToken);

            await conversationService.SaveConversationAsync(
                conversationId,
                request.Message,
                agentRunResult.AssistantMessage,
                cancellationToken);

            using (LogContext.PushProperty(
                      "ConversationId",
                      conversationId))
            {
                logger.LogInformation(
                    "Completed chat orchestration for conversation.");
            }


            return new Contracts.ChatResponse
            {
                ConversationId = conversationId,
                Answer = agentRunResult.AssistantMessage,
                UsedTools = agentRunResult.UsedTools
            };
        }
        finally
        {
            stopwatch.Stop();

            logger.LogInformation(
                "Chat completed in {ElapsedMilliseconds} ms.",
                stopwatch.ElapsedMilliseconds);
        }
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