using Agent.Api.Mcp;
using OpenAI.Chat;
using System.Text.RegularExpressions;

namespace Agent.Api.Tools;

public sealed class ToolRouter(
    IMcpToolRegistry toolRegistry)
    : IToolRouter
{
    public IReadOnlyList<ChatTool> SelectTools(
        IReadOnlyList<ChatMessage> messages)
    {
        var question =
            GetLastUserQuestion(messages);

        if (string.IsNullOrWhiteSpace(question))
        {
            return [];
        }

        var needsKnowledge =
            NeedsKnowledgeSearch(question) || IsKnowledgeFollowUp(messages, question);

        var needsCalculator =
            NeedsCalculator(question);

        var needsCurrentTime =
            NeedsCurrentTime(question);

        var tools =
            new List<ChatTool>();

        foreach (var tool in toolRegistry.Definitions)
        {
            var toolName =
                tool.FunctionName;

            switch (toolName)
            {
                case "search_knowledge":
                    if (needsKnowledge)
                    {
                        tools.Add(tool);
                    }

                    break;

                case "calculate":
                    if (needsCalculator)
                    {
                        tools.Add(tool);
                    }

                    break;

                case "get_current_time":
                    if (needsCurrentTime)
                    {
                        tools.Add(tool);
                    }

                    break;

                case "index_document":
                case "list_documents":
                case "delete_vectors":
                    break;

                default:
                    tools.Add(tool);
                    break;
            }
        }

        return tools;
    }

    private static bool IsKnowledgeFollowUp(IReadOnlyList<ChatMessage> messages, string question)
    {
        if (!Regex.IsMatch(question,
                @"(?i)\b(that|those|this|these|it|its|entitlement)\b"))
            return false;

        var previousQuestion = messages.OfType<UserChatMessage>()
            .Reverse().Skip(1).FirstOrDefault();
        return previousQuestion is not null &&
            NeedsKnowledgeSearch(string.Join(" ", previousQuestion.Content.Select(x => x.Text)));
    }
    private static string GetLastUserQuestion(
        IReadOnlyList<ChatMessage> messages)
    {
        var lastUserMessage =
            messages
                .OfType<UserChatMessage>()
                .LastOrDefault();

        return lastUserMessage?
                   .Content
                   .FirstOrDefault()?
                   .Text
                   ?.Trim()
                   .ToLowerInvariant()
               ?? string.Empty;
    }

    private static bool NeedsKnowledgeSearch(
        string question)
    {
        var knowledgeTerms =
            new[]
            {
                // Employees / staff
                "employee",
                "employees",
                "staff",
                "worker",
                "workers",
                "team member",
                "team members",

                // Vacation / leave / time off
                "vacation",
                "vacation days",
                "annual leave",
                "annual time off",
                "time off",
                "paid time off",
                "pto",
                "leave",
                "sick",
                "sick leave",

                // Remote work
                "remote work",
                "remote",
                "work from home",
                "wfh",

                // Policies / documents
                "policy",
                "policies",
                "handbook",
                "employee handbook",
                "document",
                "documents",
                "contract",
                "procedure",
                "procedures",

                // Company/internal knowledge
                "company",
                "internal",
                "organization",
                "organisation",

                // Benefits
                "benefit",
                "benefits",

                // Working conditions
                "working hours",
                "work hours",
                "office hours",
                "manager approval",

                // Contact/company people
                "ceo",
                "manager",
                "phone number",
                "contact information",
                "contact details",
                "email address"
            };

        return knowledgeTerms.Any(
            term =>
                question.Contains(
                    term,
                    StringComparison.OrdinalIgnoreCase));
    }

    private static bool NeedsCalculator(
        string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return false;
        }

        if (question.Contains("calculate") ||
            question.Contains("compute"))
        {
            return true;
        }

        return Regex.IsMatch(
            question,
            @"\d+(?:\.\d+)?\s*[\+\-\*\/]\s*\d+");
    }

    private static bool NeedsCurrentTime(
        string question)
    {
        return
            question.Contains("current time") ||
            question.Contains("what time is it") ||
            question.Contains("time now") ||
            question.Contains("current date") ||
            question.Contains("today's date");
    }
}
