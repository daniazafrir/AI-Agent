using Agent.Api.Mcp;
using Agent.Api.Tools;
using Moq;
using OpenAI.Chat;
using Xunit;

namespace Agent.Api.Tests.Tools;

public class HolidayFollowUpRoutingTests
{
    [Theory]
    [InlineData("ומה בשנה הבאה?")]
    [InlineData("„ומה בשנה הבאה?”")]
    [InlineData("\"ומה בשנה הבאה?\"")]
    [InlineData("„ומה בחו״ל?”")]
    [InlineData("ומה בחו\"ל?")]
    public void HolidayContext_SelectsToolForQuotedFollowUp(string question)
    {
        Assert.True(Select("מתי ערב שמחת תורה בשנת 2026 בישראל?",
            "ערב שמחת תורה בישראל חל באוקטובר.", question));
    }

    [Fact]
    public void RegionFollowUp_AfterYearFollowUp_SelectsTool()
    {
        Assert.True(Select("„ומה בשנה הבאה?”",
            "בשנת 2027 בישראל ערב שמחת תורה חל באוקטובר.", "„ומה בחו״ל?”"));
    }

    [Fact]
    public void NonHolidayContext_DoesNotSelectTool()
    {
        Assert.False(Select("מה מזג האוויר באילת?", "חם באילת.", "„ומה בשנה הבאה?”"));
    }

    private static bool Select(string previous, string answer, string question)
    {
        var registry = new Mock<IMcpToolRegistry>();
        registry.SetupGet(x => x.Definitions)
            .Returns(new[] { ChatTool.CreateFunctionTool("get_jewish_holidays") });
        return new ToolRouter(registry.Object).SelectTools(new ChatMessage[]
        {
            new UserChatMessage(previous), new AssistantChatMessage(answer),
            new UserChatMessage(question)
        }).Any(tool => tool.FunctionName == "get_jewish_holidays");
    }
}
