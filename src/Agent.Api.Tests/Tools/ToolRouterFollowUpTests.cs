using Agent.Api.Mcp;
using Agent.Api.Tools;
using Moq;
using OpenAI.Chat;
using Xunit;

namespace Agent.Api.Tests.Tools;

public sealed class ToolRouterFollowUpTests
{
    [Theory]
    [InlineData("How many vacation days do employees receive?", "And when does that entitlement begin?", true)]
    [InlineData("How many vacation days do employees receive?", "Explain binary search.", false)]
    [InlineData("Explain binary search.", "And when does that begin?", false)]
    public void SelectTools_UsesPreviousSubjectOnlyForFollowUp(string previous, string current, bool expected)
    {
        var search = ChatTool.CreateFunctionTool("search_knowledge");
        var registry = new Mock<IMcpToolRegistry>();
        registry.SetupGet(x => x.Definitions).Returns(new[] { search });
        var router = new ToolRouter(registry.Object);
        var tools = router.SelectTools(new ChatMessage[]
        {
            new UserChatMessage(previous),
            new AssistantChatMessage("Previous answer."),
            new UserChatMessage(current)
        });
        Assert.Equal(expected, tools.Any(x => x.FunctionName == "search_knowledge"));
    }

    [Fact]
    public void SelectTools_NewConversation_DoesNotInheritPreviousSubject()
    {
        var registry = new Mock<IMcpToolRegistry>();
        registry.SetupGet(x => x.Definitions)
            .Returns(new[] { ChatTool.CreateFunctionTool("search_knowledge") });
        var router = new ToolRouter(registry.Object);
        Assert.Empty(router.SelectTools(new ChatMessage[]
        {
            new UserChatMessage("And when does that entitlement begin?")
        }));
    }
}
