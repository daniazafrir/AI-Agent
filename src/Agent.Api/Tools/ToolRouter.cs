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
                case "get_weather":
                    if (NeedsWeather(question) || IsWeatherFollowUp(messages, question))
                        tools.Add(tool);
                    break;

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

    private static bool NeedsWeather(string question) =>
        Regex.IsMatch(question, @"(?i)\b(weather|forecast|temperature|rain|raining|snow|snowing)\b|מזג|תחזית|טמפרטור|גשם|שלג");

    private static bool IsWeatherFollowUp(IReadOnlyList<ChatMessage> messages, string question)
    {
        var users = messages.OfType<UserChatMessage>().ToArray();
        if (users.Length < 2) return false;
        var previousQuestion = string.Join(" ", users[^2].Content.Select(x => x.Text));
        // Ignore tool-round assistant messages after the latest user turn.
        var latestUserIndex = messages.ToList().FindLastIndex(x => x is UserChatMessage);
        var previousAnswer = messages.Take(latestUserIndex).OfType<AssistantChatMessage>().LastOrDefault();
        var answer = previousAnswer is null ? "" : string.Join(" ", previousAnswer.Content.Select(x => x.Text));
        if (!NeedsWeather(previousQuestion) && !NeedsWeather(answer)) return false;
        if (Regex.IsMatch(question, @"(?i)^(?:(?:and |what about |how about )?(?:tomorrow|today|the day after tomorrow)|(?:ו?מה\s+|ו)?(?:מחר|מחרתיים|היום))[?!.\s]*$"))
            return true;
        if (Regex.IsMatch(question, @"(?i)^(כן|נכון|מאשר|מאשרת|yes|correct|sure|ok|okay)[.!\s]*$"))
            return true;
        // A short location answer is relevant only after a location clarification.
        return question.Length <= 80 && !question.Contains('?') &&
            Regex.IsMatch(answer, @"(?i)(which city|which country|specify.*(?:city|country)|באיזו עיר|איזו עיר|איזו מדינה|התכוונת)");
    }

    private static bool IsKnowledgeFollowUp(IReadOnlyList<ChatMessage> messages, string question)
    {
        if (!Regex.IsMatch(question,
                @"(?i)\b(that|those|this|these|it|its|entitlement)\b|זכאות|ממתי|מתי זה|מתי היא|מתי הוא"))
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
                // Hebrew policy questions: avoid the ambiguous word עובד by itself.
                "ימי חופשה",
                "ימי החופשה",
                "חופשה שנתית",
                "מדיניות חופשה",
                "ימי מחלה",
                "עבודה מהבית",
                "עבודה מרחוק",
                "שעות העבודה",
                "שעות עבודה",
                "החזר הוצאות",
                "החזרי הוצאות",
                "נסיעות עסקיות",
                "מדיניות החברה",
                "נהלי החברה",
                "מדריך העובד",
                "מדריך לעובד",
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

                // Business travel and expense policies
                "business travel",
                "business trip",
                "reimbursement",
                "travel claim",
                "expense claim",
                "meal allowance",

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

