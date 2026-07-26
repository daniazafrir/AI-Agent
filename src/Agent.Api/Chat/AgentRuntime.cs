using System.Text.Json;
using Agent.Api.Configuration;
using Agent.Api.Mcp;
using Agent.Api.Tools;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Agent.Api.Chat;

public sealed class AgentRuntime(
    ChatClient chatClient,
    IMcpToolRegistry toolRegistry,
    IToolExecutor toolExecutor,
    IOptions<OpenAiOptions> openAiOptions,
    ILogger<AgentRuntime> logger)
    : IAgentRuntime
{
    private readonly OpenAiOptions _openAiOptions = openAiOptions.Value;

    public async Task<AgentRunResult> RunAsync(
        ICollection<ChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        await toolRegistry.InitializeAsync(cancellationToken);

        var completionOptions = CreateCompletionOptions();
        var usedTools = new List<string>();

        var maxToolRounds = Math.Clamp(
            _openAiOptions.MaxToolRounds,
            1,
            10);

        for (var round = 1; round <= maxToolRounds; round++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            logger.LogInformation(
                "Calling OpenAI; agent round {Round} of {MaxToolRounds}.",
                round,
                maxToolRounds);

            ChatCompletion completion =
                await chatClient.CompleteChatAsync(
                    messages,
                    completionOptions,
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
                    $"Unexpected OpenAI finish reason: " +
                    $"{completion.FinishReason}.");
            }

            return new AgentRunResult
            {
                AssistantMessage = GetAssistantText(completion),

                UsedTools = usedTools
                    .Distinct(StringComparer.Ordinal)
                    .ToArray()
            };
        }

        throw new InvalidOperationException(
            $"The agent exceeded the maximum number of tool rounds " +
            $"({maxToolRounds}).");
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
            cancellationToken.ThrowIfCancellationRequested();

            usedTools.Add(toolCall.FunctionName);

            var toolResult = await ExecuteToolSafelyAsync(
                toolCall.FunctionName,
                toolCall.FunctionArguments,
                cancellationToken);

            messages.Add(
                new ToolChatMessage(
                    toolCall.Id,
                    toolResult));
        }
    }

    private async Task<string> ExecuteToolSafelyAsync(
        string toolName,
        BinaryData functionArguments,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(
                "Executing agent tool {ToolName}.",
                toolName);

            return await toolExecutor.ExecuteAsync(
                toolName,
                functionArguments,
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
                toolName);

            return JsonSerializer.Serialize(new
            {
                success = false,
                tool = toolName,
                error = exception.Message
            });
        }
    }

    private static string GetAssistantText(
        ChatCompletion completion)
    {
        var text = string.Join(
            Environment.NewLine,
            completion.Content
                .Select(part => part.Text)
                .Where(textPart =>
                    !string.IsNullOrWhiteSpace(textPart)));

        return string.IsNullOrWhiteSpace(text)
            ? "The agent completed the request but returned no textual response."
            : text;
    }
}