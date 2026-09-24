using Xunit;

namespace Agent.Api.IntegrationTests.Chat;

public sealed partial class ChatAnswerAcceptanceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HebrewVacationQuestion_RetrievesEnglishPolicyAndAnswersInHebrew(bool streaming)
    {
        var result = await SendAsync("כמה ימי חופשה יש לעובד", streaming);
        Assert.Contains("search_knowledge", result.UsedTools);
        Assert.Matches(@"\b19\b", result.Answer);
        Assert.Contains("חופשה", result.Answer);
        AssertVacationSource(result);
        Assert.DoesNotContain("calculate", result.UsedTools);
    }
}
