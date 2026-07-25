using Agent.Api.Configuration;
using Agent.Api.Contracts;
using Agent.Api.Features.Chat;
using Agent.Api.Features.Conversation;
using Agent.Api.Mcp;
using Agent.Api.Tools;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.Text.Json;

namespace Agent.Api.Chat;

public sealed class ChatOrchestrator(
    ChatClient chatClient,
    IMcpToolRegistry toolRegistry,
    IToolExecutor toolExecutor,
    IConversationService conversationService,
    IOptions<AgentOptions> agentOptions,
    IOptions<OpenAiOptions> openAiOptions,
    ILogger<ChatOrchestrator> logger)
    : IChatOrchestrator
{

    private readonly AgentOptions _agentOptions = agentOptions.Value;
    private readonly OpenAiOptions _openAiOptions = openAiOptions.Value;

    public async Task<Contracts.ChatResponse> ChatAsync(
        Contracts.ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException("Message is required.", nameof(request));
        }

        var conversationId = request.ConversationId is { } suppliedId && suppliedId != Guid.Empty
            ? suppliedId
            : Guid.NewGuid();

        await toolRegistry.InitializeAsync(cancellationToken);

        var messages = await conversationService.BuildMessagesAsync(
    conversationId,
    request.Message,
    _agentOptions.SystemPrompt,
    cancellationToken);

        var options = CreateCompletionOptions();
        var usedTools = new List<string>();
        var maxToolRounds = Math.Clamp(_openAiOptions.MaxToolRounds, 1, 10);

        for (var round = 1; round <= maxToolRounds; round++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            logger.LogInformation(
                "Calling OpenAI for conversation {ConversationId}; round {Round}.",
                conversationId,
                round);

            ChatCompletion completion = await chatClient.CompleteChatAsync(
                messages,
                options,
                cancellationToken);

            if (completion.FinishReason == ChatFinishReason.ToolCalls)
            {
                messages.Add(new AssistantChatMessage(completion));

                await ExecuteToolCallsAsync(
                    completion,
                    messages,
                    usedTools,
                    cancellationToken);

                continue;
            }

            if (completion.FinishReason != ChatFinishReason.Stop)
            {
                throw new InvalidOperationException(
                    $"Unexpected OpenAI finish reason: {completion.FinishReason}.");
            }

            var assistantText = GetAssistantText(completion);

            await conversationService.SaveConversationAsync(
     conversationId,
     request.Message,
     assistantText,
     cancellationToken);

            return new Contracts.ChatResponse
            {
                ConversationId = conversationId,
                Answer = assistantText,
                UsedTools = usedTools
                    .Distinct(StringComparer.Ordinal)
                    .ToArray()
            };
        }

        throw new InvalidOperationException(
            $"The agent exceeded the maximum number of tool rounds ({maxToolRounds}).");
    }

    private ChatCompletionOptions CreateCompletionOptions()
    {
        var options = new ChatCompletionOptions();

        foreach (var tool in toolRegistry.Definitions)
        {
            options.Tools.Add(tool);
        }

        return options;
    }

    private async Task ExecuteToolCallsAsync(
        ChatCompletion completion,
        ICollection<ChatMessage> messages,
        ICollection<string> usedTools,
        CancellationToken cancellationToken)
    {
        foreach (var toolCall in completion.ToolCalls)
        {
            usedTools.Add(toolCall.FunctionName);

            string toolResult;
            try
            {
                toolResult = await toolExecutor.ExecuteAsync(
                    toolCall.FunctionName,
                    toolCall.FunctionArguments,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Tool {ToolName} failed.",
                    toolCall.FunctionName);

                toolResult = JsonSerializer.Serialize(new
                {
                    success = false,
                    tool = toolCall.FunctionName,
                    error = exception.Message
                });
            }

            messages.Add(new ToolChatMessage(toolCall.Id, toolResult));
        }
    }

    private static string GetAssistantText(ChatCompletion completion)
    {
        var text = string.Join(
            Environment.NewLine,
            completion.Content
                .Select(part => part.Text)
                .Where(part => !string.IsNullOrWhiteSpace(part)));

        return string.IsNullOrWhiteSpace(text)
            ? "The agent completed the request but returned no textual response."
            : text;
    }
}
