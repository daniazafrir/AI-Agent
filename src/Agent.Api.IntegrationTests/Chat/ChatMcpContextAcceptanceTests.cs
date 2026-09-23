using Xunit;

namespace Agent.Api.IntegrationTests.Chat;

public sealed partial class ChatAnswerAcceptanceTests
{
    [Theory]
    [InlineData(false, "What is MCP?")]
    [InlineData(true, "What is MCP?")]
    [InlineData(false, "מה זה MCP?")]
    [InlineData(true, "מה זה MCP?")]
    public async Task McpQuestion_UsesApplicationContext(bool streaming, string question)
    {
        var reply = await SendAsync(question, streaming);
        Assert.Matches("(?i)Model Context Protocol", reply.Answer);
        Assert.Empty(reply.UsedTools);
        Assert.Empty(reply.Sources);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task McpQuestion_RespectsExplicitCertificationContext(bool streaming)
    {
        var reply = await SendAsync(
            "Explain MCP specifically as Microsoft Certified Professional, the Microsoft certification.", streaming);
        Assert.Matches("(?i)Microsoft Certified Professional", reply.Answer);
        Assert.Matches("(?i)certif", reply.Answer);
        Assert.Empty(reply.UsedTools);
        Assert.Empty(reply.Sources);
    }
}
