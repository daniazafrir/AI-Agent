using Agent.Api.Features.Conversation;
using OpenAI.Chat;

namespace Agent.Api.Chat.Models;

public sealed class AgentContext
{
    public required Guid ConversationId { get; init; }

    public required List<ChatMessage> Messages { get; init; }


    public List<string> UsedTools { get; } = [];

    public int Round { get; set; }

    public List<KnowledgeSource> Sources { get; init; } = [];
    public ChatDebugInfo? Debug { get; set; }
    public List<PromptSnapshot> Prompts { get; } = [];
    public List<ToolTrace> ToolCalls { get; } = [];
    public bool EnglishKnowledgeRetryPending { get; set; }
    public bool EnglishKnowledgeRetryRequested { get; set; }
}
