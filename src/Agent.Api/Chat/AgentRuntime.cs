using Agent.Api.Configuration;
using Agent.Api.Mcp;
using Agent.Api.OpenAI;
using Agent.Api.Tools;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.Text.Json;

namespace Agent.Api.Chat;

public sealed class AgentRuntime(
    IChatCompletionService chatCompletionService,
    IMcpToolRegistry toolRegistry,
    IToolExecutor toolExecutor,
    IOptions<OpenAiOptions> openAiOptions,
    ILogger<AgentRuntime> logger)
    : IAgentRuntime
{
    private const string EmptyAssistantResponse =
        "The agent completed the request but returned no textual response.";

    private readonly OpenAiOptions _openAiOptions = openAiOptions.Value;

    public async Task<AgentRunResult> RunAsync(
      ICollection<ChatMessage> messages,
      CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        await InitializeToolsAsync(cancellationToken);

        var context = CreateContext(messages);

        while (context.CurrentRound < context.MaxRounds)
        {
            context.NextRound();

            ChatCompletionResult completion =
                await CompleteChatAsync(context, cancellationToken);

            if (completion.FinishReason == ChatFinishReason.Stop)
            {
                return BuildResult(completion, context);
            }

            if (completion.FinishReason == ChatFinishReason.ToolCalls)
            {
                if (completion.ToolCalls.Count == 0)
                {
                    throw new InvalidOperationException(
                        "OpenAI returned ToolCalls finish reason without any tool calls.");
                }

                await ExecuteToolCallsAsync(
                    completion,
                    context,
                    cancellationToken);

                continue;
            }

            throw new InvalidOperationException(
                $"Unsupported finish reason: {completion.FinishReason}");
        }

        throw new InvalidOperationException(
            $"Agent exceeded the maximum number of {context.MaxRounds} rounds.");
    }

    private Task InitializeToolsAsync(
        CancellationToken cancellationToken)
    {
        return toolRegistry.InitializeAsync(cancellationToken);
    }

    private AgentContext CreateContext(
        ICollection<ChatMessage> messages)
    {
        var maxRounds = Math.Clamp(
            _openAiOptions.MaxToolRounds,
            1,
            10);

        return new AgentContext(
            messages,
            CreateCompletionOptions(),
            maxRounds);
    }

    private ChatCompletionOptions CreateCompletionOptions()
    {
        var options = new ChatCompletionOptions();

        foreach (var toolDefinition in toolRegistry.Definitions)
        {
            options.Tools.Add(toolDefinition);
        }

        return options;
    }

    private Task<ChatCompletionResult> CompleteChatAsync(
    AgentContext context,
    CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Calling OpenAI; agent round {CurrentRound} of {MaxRounds}.",
            context.CurrentRound,
            context.MaxRounds);

        return chatCompletionService.CompleteAsync(
            context.Messages,
            context.CompletionOptions,
            cancellationToken);
    }
    private async Task ExecuteToolCallsAsync(
    ChatCompletionResult completion,
    AgentContext context,
    CancellationToken cancellationToken)
    {
        foreach (ToolCallResult toolCall in completion.ToolCalls)
        {
            if (string.IsNullOrWhiteSpace(toolCall.Id))
            {
                throw new InvalidOperationException(
                    "Tool call ID cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(toolCall.Name))
            {
                throw new InvalidOperationException(
                    "Tool call name cannot be empty.");
            }

            string toolResult = await ExecuteToolAsync(
                toolCall.Name,
                toolCall.Arguments,
                cancellationToken);

            toolResult ??= string.Empty;

            context.Messages.Add(
                new ToolChatMessage(
                    toolCall.Id,
                    toolResult));

            context.UsedTools.Add(toolCall.Name);
        }
    }

    private async Task<string> ExecuteToolAsync(
        string toolName,
        BinaryData functionArguments,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(
                "Executing tool {ToolName}.",
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

    private static AgentRunResult BuildResult(
        ChatCompletionResult completion,
        AgentContext context)
    {
        return new AgentRunResult
        {
            AssistantMessage = GetAssistantMessage(completion),

            UsedTools = context.UsedTools
                .Distinct(StringComparer.Ordinal)
                .ToArray()
        };
    }

    private static string GetAssistantMessage(
        ChatCompletionResult completion)
    {
        return string.IsNullOrWhiteSpace(completion.AssistantMessage)
            ? EmptyAssistantResponse
            : completion.AssistantMessage;
    }

    
    private sealed class AgentContext(
        ICollection<ChatMessage> messages,
        ChatCompletionOptions completionOptions,
        int maxRounds)
    {
        public ICollection<ChatMessage> Messages { get; } = messages;

        public ChatCompletionOptions CompletionOptions { get; }
            = completionOptions;

        public List<string> UsedTools { get; } = [];

        public int CurrentRound { get; private set; }

        public int MaxRounds { get; } = maxRounds;
        public void NextRound()
        {
            CurrentRound++;
        }
        
    }
}
