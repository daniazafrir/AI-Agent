using Agent.Api.Mcp;
using Agent.Api.Tools;
using Moq;
using OpenAI.Chat;
using Xunit;

namespace Agent.Api.Tests.Tools;

public sealed class ToolRouterTravelTests
{
    [Theory]
    [InlineData("What is the daily meal reimbursement limit for business travel?", true)]
    [InlineData("When must I submit a travel reimbursement claim?", true)]
    [InlineData("When are approved travel claims paid?", true)]
    [InlineData("What is the mileage reimbursement rate?", true)]
    [InlineData("What is the meal allowance?", true)]
    [InlineData("How does light travel through glass?", false)]
    [InlineData("Suggest a meal recipe.", false)]
    public void SelectTools_OffersKnowledgeSearchForTravelPolicies(string question, bool expected)
    {
        var registry = new Mock<IMcpToolRegistry>();
        registry.SetupGet(x => x.Definitions).Returns(new[]
        {
            ChatTool.CreateFunctionTool("search_knowledge"),
            ChatTool.CreateFunctionTool("calculate")
        });
        var router = new ToolRouter(registry.Object);
        var tools = router.SelectTools(new ChatMessage[] { new UserChatMessage(question) });
        Assert.Equal(expected, tools.Any(x => x.FunctionName == "search_knowledge"));
        Assert.DoesNotContain(tools, x => x.FunctionName == "calculate");
    }
}
