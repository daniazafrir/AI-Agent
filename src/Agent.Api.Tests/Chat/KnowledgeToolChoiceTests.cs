using Agent.Api.Chat;
using Agent.Api.Chat.Models;
using Agent.Api.Configuration;
using Agent.Api.Mcp;
using Agent.Api.OpenAI;
using Agent.Api.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using OpenAI.Chat;
using System.Reflection;
using Xunit;

namespace Agent.Api.Tests.Chat;

public sealed class KnowledgeToolChoiceTests
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void BuildOptions_RequiresFreshSearchBeforeAnswer(bool knowledge, bool alreadySearched)
    {
        var router = new Mock<IToolRouter>();
        router.Setup(x => x.SelectTools(It.IsAny<IReadOnlyList<ChatMessage>>()))
            .Returns(new[] { ChatTool.CreateFunctionTool(knowledge ? "search_knowledge" : "calculate") });
        var loop = new AgentLoop(
            Mock.Of<IChatCompletionService>(), Mock.Of<IMcpToolRegistry>(),
            Mock.Of<IToolProcessor>(), Mock.Of<IToolExecutor>(),
            Options.Create(new OpenAiOptions()), router.Object,
            NullLogger<AgentLoop>.Instance);
        var context = new AgentContext
        {
            ConversationId = Guid.NewGuid(),
            Messages = new List<ChatMessage> { new UserChatMessage("When must I submit a travel reimbursement claim?") }
        };
        if (alreadySearched) context.UsedTools.Add("search_knowledge");
        var options = (ChatCompletionOptions)typeof(AgentLoop)
            .GetMethod("BuildOptions", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(loop, new object[] { context })!;
        var expected = knowledge && !alreadySearched
            ? ChatToolChoice.CreateFunctionChoice("search_knowledge")
            : ChatToolChoice.CreateAutoChoice();
        Assert.Equal(System.ClientModel.Primitives.ModelReaderWriter.Write(expected).ToString(),
            System.ClientModel.Primitives.ModelReaderWriter.Write(options.ToolChoice!).ToString());
    }
}

