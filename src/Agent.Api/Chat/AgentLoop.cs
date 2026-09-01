using Agent.Api.Configuration;
using Agent.Api.Features.Conversation;
using Agent.Api.Mcp;
using Agent.Api.OpenAI;
using Agent.Api.Tools;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Agent.Api.Chat;

public sealed class AgentLoop(
    IChatCompletionService completionService,
    IMcpToolRegistry toolRegistry,
    IToolProcessor toolProcessor,
    IToolExecutor toolExecutor,
    IOptions<OpenAiOptions> options,
    IToolRouter toolRouter,
    ILogger<AgentLoop> logger)
    : IAgentLoop
{
    private readonly OpenAiOptions _options =
        options.Value;

    public async Task<AgentRunResult> RunAsync(
     AgentContext context,
     CancellationToken cancellationToken)
    {
        await toolRegistry.InitializeAsync(
            cancellationToken);

        var chatOptions =
            BuildOptions(context);

        while (
            context.Round <
            _options.MaxToolRounds)
        {
            context.Round++;

            var completion =
                await completionService.CompleteAsync(
                    context.Messages,
                    chatOptions,
                    cancellationToken);

            if (
                completion.FinishReason ==
                ChatFinishReason.ToolCalls)
            {
                if (completion.ToolCalls.Count == 0)
                {
                    throw new InvalidOperationException(
                        "OpenAI returned ToolCalls finish reason without any tool calls.");
                }

                await toolProcessor.ProcessAsync(
                    completion,
                    context,
                    cancellationToken);

                continue;
            }

            if (
                completion.FinishReason ==
                ChatFinishReason.Stop)
            {
                var assistantMessage =
                    string.IsNullOrWhiteSpace(
                        completion.AssistantMessage)
                        ? "The agent completed the request but returned no textual response."
                        : completion.AssistantMessage;

                return new AgentRunResult
                {
                    AssistantMessage =
        assistantMessage,

                    UsedTools =
        context.UsedTools
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .ToList(),

                    Sources =
        context.Sources
            .DistinctBy(x => x.DocumentName)
            .ToList()
                };
            }

            throw new InvalidOperationException(
                $"Unsupported finish reason: {completion.FinishReason}.");
        }

        throw new InvalidOperationException(
            $"Agent exceeded the maximum number of {_options.MaxToolRounds} rounds.");
    }

    public async IAsyncEnumerable<ChatStreamEvent> RunStreamingAsync(
    AgentContext context,
    [System.Runtime.CompilerServices.EnumeratorCancellation]
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        await toolRegistry.InitializeAsync(
            cancellationToken);

        var chatOptions =
            BuildOptions(context);

        yield return new ChatStreamEvent
        {
            Type = "started"
        };

        while (context.Round < _options.MaxToolRounds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            context.Round++;


            chatOptions.ToolChoice =
    ChatToolChoice.CreateAutoChoice();

            var finishReason =
                ChatFinishReason.Stop;

            var hasFinishReason =
                false;

            var toolCallBuilders =
                new Dictionary<int, StreamingToolCallBuilder>();
            logger.LogInformation(
    "Round {Round}. Available tools: {Tools}",
    context.Round,
    string.Join(
        ", ",
        chatOptions.Tools.Select(
            x => x.FunctionName)));
            await foreach (
                var update in
                    completionService
                        .CompleteStreamingAsync(
                            context.Messages,
                            chatOptions,
                            cancellationToken)
                        .WithCancellation(
                            cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                //
                // text chunks
                //
                foreach (var contentPart in update.ContentUpdate)
                {
                    if (string.IsNullOrEmpty(contentPart.Text))
                    {
                        continue;
                    }

                    yield return new ChatStreamEvent
                    {
                        Type = "content",
                        Content = contentPart.Text
                    };
                }

                //
                // tool call chunks
                //
                foreach (var toolUpdate in update.ToolCallUpdates)
                {
                    if (!toolCallBuilders.TryGetValue(
                            toolUpdate.Index,
                            out var builder))
                    {
                        builder =
                            new StreamingToolCallBuilder();

                        toolCallBuilders[
                            toolUpdate.Index] = builder;
                    }

                    if (!string.IsNullOrWhiteSpace(
                            toolUpdate.ToolCallId))
                    {
                        builder.Id =
                            toolUpdate.ToolCallId;
                    }

                    if (!string.IsNullOrWhiteSpace(
                            toolUpdate.FunctionName))
                    {
                        builder.Name =
                            toolUpdate.FunctionName;
                    }

                    if (
                        toolUpdate.FunctionArgumentsUpdate
                        is not null)
                    {
                        builder.Arguments.Append(
                            toolUpdate
                                .FunctionArgumentsUpdate
                                .ToString());
                    }
                }

                if (update.FinishReason.HasValue)
                {
                    finishReason =
                        update.FinishReason.Value;

                    hasFinishReason =
                        true;
                }
            }

            if (!hasFinishReason)
            {
                throw new InvalidOperationException(
                    "OpenAI streaming response completed without a finish reason.");
            }

            //
            // final assistant answer
            //
            if (finishReason == ChatFinishReason.Stop)
            {
                yield return new ChatStreamEvent
                {
                    Type = "completed",

                    UsedTools =
         context.UsedTools
             .Distinct(
                 StringComparer.OrdinalIgnoreCase)
             .ToList(),

                    Sources =
         context.Sources
             .DistinctBy(x => new
             {
                 x.DocumentName,
                 x.ChunkIndex
             })
             .ToList()
                };

                yield break;
            }

            //
            // tool calls
            //
            if (finishReason == ChatFinishReason.ToolCalls)
            {
                if (toolCallBuilders.Count == 0)
                {
                    throw new InvalidOperationException(
                        "OpenAI returned ToolCalls finish reason without any streamed tool calls.");
                }

                var toolCalls =
                    toolCallBuilders
                        .OrderBy(x => x.Key)
                        .Select(x =>
                            ChatToolCall.CreateFunctionToolCall(
                                x.Value.Id,
                                x.Value.Name,
                                BinaryData.FromString(
                                    x.Value.Arguments
                                        .ToString())))
                        .ToList();

                //
                // IMPORTANT:
                // assistant message must precede tool messages
                //
                var assistantMessage =
                    new AssistantChatMessage(
                        toolCalls);

                context.Messages.Add(
                    assistantMessage);

                foreach (var toolCall in toolCalls)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    yield return new ChatStreamEvent
                    {
                        Type = "tool-started",
                        ToolName =
                            toolCall.FunctionName
                    };

                    if (!context.UsedTools.Contains(
                            toolCall.FunctionName,
                            StringComparer.OrdinalIgnoreCase))
                    {
                        context.UsedTools.Add(
                            toolCall.FunctionName);
                    }

                    logger.LogInformation(
                    "Executing tool {Tool} with arguments:\n{Arguments}",
                    toolCall.FunctionName,
                    toolCall.FunctionArguments);

                    var result =
                        await toolExecutor.ExecuteAsync(
                            toolCall.FunctionName,
                            toolCall.FunctionArguments,
                            cancellationToken);

                    if (string.Equals(
        toolCall.FunctionName,
        "search_knowledge",
        StringComparison.OrdinalIgnoreCase))
                    {
                        ExtractSources(
                            result.RawContent,
                            context.Sources);
                    }

                    logger.LogInformation(
                        "Tool {Tool} result:\n{Result}",
                        toolCall.FunctionName,
                        result);

                    context.Messages.Add(
                        new ToolChatMessage(
                            toolCall.Id,
                            result.Content));

                    logger.LogInformation(
    "RawContent: {RawContent}",
    result.RawContent);

                    logger.LogInformation(
                        "Normalized Content: {Content}",
                        result.Content);

                    logger.LogInformation(
                        "Sources count after extraction: {Count}",
                        context.Sources.Count);

                    yield return new ChatStreamEvent
                    {
                        Type = "tool-completed",
                        ToolName =
                            toolCall.FunctionName
                    };
                }

                continue;
            }

            throw new InvalidOperationException(
                $"Unsupported streaming finish reason: {finishReason}.");
        }

        throw new InvalidOperationException(
            $"Agent exceeded the maximum number of {_options.MaxToolRounds} rounds.");
    }


    private ChatCompletionOptions BuildOptions(
    AgentContext context)
    {
        var options =
            new ChatCompletionOptions
            {
                ToolChoice =
                    ChatToolChoice.CreateAutoChoice()
            };

        foreach (var tool in
                 toolRouter.SelectTools(
                     context.Messages))
        {
            options.Tools.Add(tool);
        }

        return options;
    }
    private sealed class StreamingToolCallBuilder
    {
        public string Id { get; set; } =
            string.Empty;

        public string Name { get; set; } =
            string.Empty;

        public System.Text.StringBuilder Arguments { get; } =
            new();
    }

    private static void ExtractSources(
    string result,
    ICollection<KnowledgeSource> sources)
    {
        try
        {
            using var document =
                JsonDocument.Parse(result);

            if (!document.RootElement.TryGetProperty(
                    "matches",
                    out var matches) ||
                matches.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var match in matches.EnumerateArray())
            {
                var documentId =
                    match.GetProperty("documentId")
                        .GetGuid();

                var documentName =
                    match.GetProperty("documentName")
                        .GetString();

                var chunkIndex =
                    match.GetProperty("chunkIndex")
                        .GetInt32();

                var score =
                    match.GetProperty("score")
                        .GetDouble();

                if (string.IsNullOrWhiteSpace(documentName))
                {
                    continue;
                }

                sources.Add(
                    new KnowledgeSource
                    {
                        DocumentId = documentId,
                        DocumentName = documentName,
                        ChunkIndex = chunkIndex,
                        Score = score
                    });
            }
        }
        catch (JsonException)
        {
            // ignore invalid tool payload
        }
    }
}
