using Agent.Api.Mcp;
using Agent.Api.Tools;
using Moq;
using OpenAI.Chat;
using Xunit;

namespace Agent.Api.Tests.Tools;

public class HebrewKnowledgeRoutingTests
{
    [Theory]
    [InlineData("כמה ימי חופשה יש לעובד", true)]
    [InlineData("מה מספר ימי החופשה שלנו?", true)]
    [InlineData("כמה ימי מחלה מגיעים לי?", true)]
    [InlineData("מה מדיניות עבודה מהבית?", true)]
    [InlineData("איך מגישים החזר הוצאות?", true)]
    [InlineData("איך עובד MCP?", false)]
    [InlineData("מה מזג האוויר באילת?", false)]
    public void HebrewQuestion_SelectsKnowledgeOnlyForPolicy(string question, bool expected)
    {
        Assert.Equal(expected, Select(new UserChatMessage(question)));
    }

    [Theory]
    [InlineData("כמה ימי חופשה יש לעובד", true)]
    [InlineData("איך עובד MCP?", false)]
    public void HebrewFollowUp_UsesPreviousPolicyContext(string previous, bool expected)
    {
        Assert.Equal(expected, Select(new UserChatMessage(previous),
            new AssistantChatMessage("תשובה קודמת"), new UserChatMessage("ממתי מתחילה הזכאות?")));
    }

    private static bool Select(params ChatMessage[] messages)
    {
        var registry = new Mock<IMcpToolRegistry>();
        registry.SetupGet(x => x.Definitions).Returns(new[] { ChatTool.CreateFunctionTool("search_knowledge") });
        return new ToolRouter(registry.Object).SelectTools(messages)
            .Any(tool => tool.FunctionName == "search_knowledge");
    }
}
