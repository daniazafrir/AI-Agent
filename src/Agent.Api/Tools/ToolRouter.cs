using Agent.Api.Mcp;
using OpenAI.Chat;

namespace Agent.Api.Tools;

public sealed class ToolRouter(
    IMcpToolRegistry toolRegistry)
    : IToolRouter
{
    public IReadOnlyList<ChatTool> SelectTools(
        IReadOnlyList<ChatMessage> messages)
    {
        var tools =
            new List<ChatTool>();

        var lastUserMessage =
            messages
                .OfType<UserChatMessage>()
                .LastOrDefault();

        var question =
            lastUserMessage?
                .Content
                .FirstOrDefault()?
                .Text
                ?.ToLowerInvariant()
            ?? string.Empty;

        var isGeneralKnowledgeQuestion =
    question.StartsWith("what is ") ||
    question.StartsWith("who is ") ||
    question.StartsWith("define ");

        var needsKnowledge =
            !isGeneralKnowledgeQuestion &&
            (
                question.Contains("employee") ||
                question.Contains("vacation") ||
                question.Contains("leave") ||
                question.Contains("remote") ||
                question.Contains("policy") ||
                question.Contains("handbook") ||
                question.Contains("document")
            );

        foreach (var tool in toolRegistry.Definitions)
        {
            if (!needsKnowledge &&
                tool.FunctionName is
                "search_knowledge" or
                "list_documents" or
                "delete_vectors")
            {
                continue;
            }

            tools.Add(tool);
        }

        return tools;
    }
}