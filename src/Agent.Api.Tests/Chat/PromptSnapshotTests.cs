using System.Text.Json;
using Agent.Api.Chat.Models;
using OpenAI.Chat;
using Xunit;

namespace Agent.Api.Tests.Chat;

public class PromptSnapshotTests
{
    [Fact]
    public void Capture_PreservesRolesToolCallLinkAndExactToolResult()
    {
        var call = ChatToolCall.CreateFunctionToolCall("call1", "search_knowledge",
            BinaryData.FromString("""{"query":"vacation"}"""));
        List<ChatMessage> messages = [
            new SystemChatMessage("system instructions"),
            new UserChatMessage("How many days?"),
            new AssistantChatMessage(new[] { call }),
            new ToolChatMessage("call1", "Source: Handbook\nEmployees get 19 days.")
        ];
        var options = new ChatCompletionOptions { ToolChoice = ChatToolChoice.CreateAutoChoice() };
        options.Tools.Add(ChatTool.CreateFunctionTool("search_knowledge", "Search",
            BinaryData.FromString("""{"type":"object","properties":{"query":{"type":"string"}}}""")));
        var snapshot = PromptSnapshot.Capture(2, "test-model", messages, options);
        using var json = JsonDocument.Parse(snapshot.MessagesJson);
        Assert.Equal("system", json.RootElement[0].GetProperty("role").GetString());
        Assert.Equal("call1", json.RootElement[2].GetProperty("tool_calls")[0].GetProperty("id").GetString());
        Assert.Equal("call1", json.RootElement[3].GetProperty("tool_call_id").GetString());
        Assert.Contains("Employees get 19 days.", snapshot.MessagesJson);
        using var settings = JsonDocument.Parse(snapshot.OptionsJson);
        Assert.Equal("search_knowledge", settings.RootElement.GetProperty("tools")[0].GetProperty("function").GetProperty("name").GetString());
        Assert.Equal("auto", settings.RootElement.GetProperty("tool_choice").GetString());
        messages.Clear();
        options.Tools.Clear();
        Assert.Equal(4, JsonDocument.Parse(snapshot.MessagesJson).RootElement.GetArrayLength());
        Assert.Contains("search_knowledge", snapshot.OptionsJson);
    }
}
