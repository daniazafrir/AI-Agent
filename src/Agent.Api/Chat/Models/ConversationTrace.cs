using Agent.Api.Features.Conversation;
using System.Text.Json;

namespace Agent.Api.Chat.Models;

public sealed record ToolTrace(string Name, string Arguments, string Result, ChatDebugInfo? Debug,
    IReadOnlyList<KnowledgeSource>? Sources = null);

public sealed record ConversationTrace
{
    public int Version { get; init; } = 1;
    public string Status { get; init; } = "completed";
    public DateTimeOffset StartedAtUtc { get; init; }
    public long TotalMs { get; init; }
    public long? FirstTextMs { get; init; }
    public IReadOnlyList<PromptSnapshot> Prompts { get; init; } = [];
    public IReadOnlyList<ToolTrace> ToolCalls { get; init; } = [];
    public IReadOnlyList<string> UsedTools { get; init; } = [];
    public IReadOnlyList<KnowledgeSource> Sources { get; init; } = [];
    public ChatDebugInfo? Debug { get; init; }
    public string ToJson() => JsonSerializer.Serialize(this);
    public static ConversationTrace? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<ConversationTrace>(json); }
        catch (JsonException) { return null; }
    }
}
