using Agent.Api.Chat.Models;
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

        while (
            context.Round <
            _options.MaxToolRounds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            context.Round++;

            var chatOptions =
                BuildOptions(context);

            logger.LogInformation(
                "Round {Round}. Available tools: {Tools}",
                context.Round,
                string.Join(
                    ", ",
                    chatOptions.Tools.Select(
                        x => x.FunctionName)));

            if (_options.EnablePromptViewer)
                context.Prompts.Add(PromptSnapshot.Capture(
                    context.Round, _options.Model, context.Messages, chatOptions));

            var completion =
                await completionService.CompleteAsync(
                    context.Messages,
                    chatOptions,
                    cancellationToken);

            if (
                completion.FinishReason ==
                ChatFinishReason.ToolCalls || (completion.FinishReason == ChatFinishReason.Stop && completion.ToolCalls.Count > 0))
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
                    Prompts = context.Prompts.ToArray(),
                    Debug = context.Debug,
                    AssistantMessage =
                        assistantMessage,

                    UsedTools =
                        context.UsedTools
                            .Distinct(
                                StringComparer.OrdinalIgnoreCase)
                            .ToList(),

                    Sources =
                        context.Sources
                            .DistinctBy(
                                x => new
                                {
                                    x.DocumentName,
                                    x.ChunkIndex
                                })
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

        var executedToolCalls =
    new HashSet<string>(
        StringComparer.Ordinal);

        yield return new ChatStreamEvent
        {
            Type = "started"
        };

        while (context.Round < _options.MaxToolRounds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            context.Round++;

            var chatOptions =
                BuildOptions(context);

            if (_options.EnablePromptViewer)
                yield return new ChatStreamEvent
                {
                    Type = "prompt",
                    Prompt = PromptSnapshot.Capture(
                        context.Round, _options.Model, context.Messages, chatOptions)
                };

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

            logger.LogInformation(
                "Streaming round {Round}: FinishReason={FinishReason}, ToolCalls={ToolCallCount}",
                context.Round, finishReason, toolCallBuilders.Count);

            if (!hasFinishReason)
            {
                throw new InvalidOperationException(
                    "OpenAI streaming response completed without a finish reason.");
            }

            //
            // final assistant answer
            //
            if (finishReason == ChatFinishReason.Stop && toolCallBuilders.Count == 0)
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
                        .ToList(),

                    Debug = context.Debug
                };

                yield break;
            }

            //
            // tool calls
            //
            if (finishReason == ChatFinishReason.ToolCalls || (finishReason == ChatFinishReason.Stop && toolCallBuilders.Count > 0))
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

                    var toolCallKey =
    CreateToolCallKey(
        toolCall.FunctionName,
        toolCall.FunctionArguments);

                    if (!executedToolCalls.Add(toolCallKey))
                    {
                        logger.LogWarning(
                            "Duplicate tool call blocked. Tool: {Tool}, Arguments: {Arguments}",
                            toolCall.FunctionName,
                            toolCall.FunctionArguments);

                        context.Messages.Add(
                            new ToolChatMessage(
                                toolCall.Id,
                                """
            {
              "success": false,
              "error": "This exact tool call was already executed. Do not repeat it. Use the previous tool result and provide the final answer."
            }
            """));

                        yield return new ChatStreamEvent
                        {
                            Type = "tool-completed",
                            ToolName = toolCall.FunctionName
                        };

                        continue;
                    }
                  

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

                        KnowledgeSearchRetry.Observe(context, toolCall.FunctionArguments, result.RawContent);

                        context.Debug =
                            ExtractDebugInfo(
                                result.RawContent,
                                toolCall.FunctionArguments, result.Analytics);
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

    internal static ChatDebugInfo ExtractDebugInfo(
     string rawContent,
     BinaryData functionArguments,
     KnowledgeContextAnalytics? analytics = null)
    {
        var query = string.Empty;
        var matches = new List<ChatSourceInfo>();

        var rawVectorResults = 0;
        var relevantVectorResults = 0;
        var vectorResults = 0;
        var keywordResults = 0;
        var mergedResults = 0;

        var minimumVectorScore = 0.0;
        var searchTimeMs = 0L;

        try
        {
            using var argsDocument =
                JsonDocument.Parse(
                    functionArguments.ToString());

            if (argsDocument.RootElement.TryGetProperty(
                "query",
                out var queryElement))
            {
                query =
                    queryElement.GetString() ??
                    string.Empty;
            }

            using var resultDocument =
                JsonDocument.Parse(rawContent);

            var root =
                resultDocument.RootElement;

            if (root.TryGetProperty("matches", out var results) && results.ValueKind == JsonValueKind.Array)
            {
                foreach (var match in results.EnumerateArray())
                {
                    var engine = match.TryGetProperty("searchEngine", out var e)
                        ? e.ValueKind == JsonValueKind.String ? e.GetString() ?? ""
                            : e.ValueKind == JsonValueKind.Number && e.TryGetInt32(out var code)
                                ? Enum.GetName(typeof(Agent.Knowledge.Search.Models.SearchEngineType), code) ?? "Unknown"
                                : "Unknown"
                        : "Unknown";
                    matches.Add(new ChatSourceInfo
                    {
                        DocumentName = match.TryGetProperty("documentName", out var name) ? name.GetString() ?? "" : "",
                        ChunkIndex = match.TryGetProperty("chunkIndex", out var index) ? index.GetInt32() : 0,
                        Score = match.TryGetProperty("score", out var score) ? score.GetDouble() : 0,
                        SearchEngine = engine
                    });
                }
            }

            if (root.TryGetProperty(
                "rawVectorResults",
                out var rawVectorElement))
            {
                rawVectorResults =
                    rawVectorElement.GetInt32();
            }

            if (root.TryGetProperty(
                "relevantVectorResults",
                out var relevantVectorElement))
            {
                relevantVectorResults =
                    relevantVectorElement.GetInt32();
            }

            if (root.TryGetProperty(
                "vectorResults",
                out var vectorElement))
            {
                vectorResults =
                    vectorElement.GetInt32();
            }

            if (root.TryGetProperty(
                "keywordResults",
                out var keywordElement))
            {
                keywordResults =
                    keywordElement.GetInt32();
            }

            if (root.TryGetProperty(
                "mergedResults",
                out var mergedElement))
            {
                mergedResults =
                    mergedElement.GetInt32();
            }

            if (root.TryGetProperty(
                "minimumVectorScore",
                out var minimumVectorScoreElement))
            {
                minimumVectorScore =
                    minimumVectorScoreElement.GetDouble();
            }

            if (root.TryGetProperty(
                "searchTimeMs",
                out var timeElement))
            {
                searchTimeMs =
                    timeElement.GetInt64();
            }
        }
        catch (JsonException)
        {
            // ignore malformed debug payload
        }

        return new ChatDebugInfo
        {
            Analytics = analytics,
            ToolName = "search_knowledge",
            Matches = matches,
            Query = query,

            RawVectorResults = rawVectorResults,
            RelevantVectorResults = relevantVectorResults,
            VectorResults = vectorResults,

            KeywordResults = keywordResults,
            MergedResults = mergedResults,

            MinimumVectorScore = minimumVectorScore,

            SearchTimeMs = searchTimeMs,
            EmbeddingTimeMs = ReadTiming(rawContent, "embeddingTimeMs"),
            VectorSearchTimeMs = ReadTiming(rawContent, "vectorSearchTimeMs"),
            KeywordSearchTimeMs = ReadTiming(rawContent, "keywordSearchTimeMs"),
            RankingTimeMs = ReadTiming(rawContent, "rankingTimeMs"),
        };
    }
    private static long? ReadTiming(string rawContent, string name)
    {
        try
        {
            using var document = JsonDocument.Parse(rawContent);
            return TryGetPropertyIgnoreCase(document.RootElement, name, out var value) &&
                value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var ms) && ms >= 0
                ? ms : null;
        }
        catch (JsonException) { return null; }
    }

    private ChatCompletionOptions BuildOptions(
    AgentContext context)
    {
        var selectedTools =
            toolRouter.SelectTools(context.Messages);

        var options =
            new ChatCompletionOptions();

        foreach (var tool in selectedTools)
        {
            options.Tools.Add(tool);
        }

        if (selectedTools.Count > 0)
        {
            var retryKnowledge = context.EnglishKnowledgeRetryPending &&
                !context.EnglishKnowledgeRetryRequested &&
                selectedTools.Any(tool => tool.FunctionName == "search_knowledge");
            if (retryKnowledge)
            {
                context.EnglishKnowledgeRetryPending = false;
                context.EnglishKnowledgeRetryRequested = true;
                context.Messages.Add(new SystemChatMessage("The Hebrew knowledge search returned no matches. Call search_knowledge now with an equivalent concise English query, preserving the user's intent and constraints. Do not ask permission or announce a future search. After the tool result, answer in the user's language using only retrieved evidence."));
            }
            options.ToolChoice =
                selectedTools.Any(tool => tool.FunctionName == "search_knowledge") &&
                (retryKnowledge || !context.UsedTools.Contains("search_knowledge", StringComparer.OrdinalIgnoreCase))
                    ? ChatToolChoice.CreateFunctionChoice("search_knowledge")
                    : selectedTools.Any(tool => tool.FunctionName == "get_weather") &&
                      !context.UsedTools.Contains("get_weather", StringComparer.OrdinalIgnoreCase)
                        ? ChatToolChoice.CreateFunctionChoice("get_weather")
                        : ChatToolChoice.CreateAutoChoice();
        }

        logger.LogInformation(
            "Round {Round}. Available tools: {Tools}",
            context.Round,
            selectedTools.Count == 0
                ? "(none)"
                : string.Join(
                    ", ",
                    selectedTools.Select(x => x.FunctionName)));

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

    internal static void ExtractSources(
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
                if (!TryGetPropertyIgnoreCase(
                        match,
                        "documentId",
                        out var documentIdElement) ||
                    !documentIdElement.TryGetGuid(out var documentId) ||
                    !TryGetPropertyIgnoreCase(
                        match,
                        "documentName",
                        out var documentNameElement) ||
                    !TryGetPropertyIgnoreCase(
                        match,
                        "chunkIndex",
                        out var chunkIndexElement) ||
                    !chunkIndexElement.TryGetInt32(out var chunkIndex) ||
                    !TryGetPropertyIgnoreCase(
                        match,
                        "score",
                        out var scoreElement) ||
                    !scoreElement.TryGetDouble(out var score))
                {
                    continue;
                }

                var documentName = documentNameElement.GetString();

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

    private static bool TryGetPropertyIgnoreCase(
        JsonElement element,
        string propertyName,
        out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(
                        property.Name,
                        propertyName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static string CreateToolCallKey(
    string toolName,
    BinaryData arguments)
    {
        var normalizedArguments =
            NormalizeJson(arguments.ToString());

        return
            $"{toolName.ToLowerInvariant()}:{normalizedArguments}";
    }

    private static string NormalizeJson(
        string json)
    {
        try
        {
            using var document =
                JsonDocument.Parse(json);

            return JsonSerializer.Serialize(
                document.RootElement);
        }
        catch (JsonException)
        {
            return json.Trim();
        }
    }
}




