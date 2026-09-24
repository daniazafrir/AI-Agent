using System.Globalization;
using System.Text.RegularExpressions;
using Xunit;

namespace Agent.Api.IntegrationTests.Chat;

public sealed partial class ChatAnswerAcceptanceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Weather_EilatThenTomorrow_CallsToolOnEachTurn(bool streaming)
    {
        var id = Guid.NewGuid();
        var timezone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Jerusalem");
        var dateBefore = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timezone).Date;
        const string question = "מה מזג האוויר באילת?";
        const string followUp = "ומה מחר?";

        var current = await SendAsync(question, streaming, id);
        AssertWeatherAnswer(current);
        var tomorrow = await SendAsync(followUp, streaming, id);
        AssertWeatherAnswer(tomorrow);

        var dateAfter = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timezone).Date;
        // Accept either local date if the live test crosses midnight.
        var expectedDates = new[] { dateBefore.AddDays(1), dateAfter.AddDays(1) };
        Assert.True(expectedDates.Any(date => ContainsDate(tomorrow.Answer, date)),
            "The follow-up must state tomorrow's calendar date in Eilat. Answer: " + tomorrow.Answer);
        await AssertHistoryAsync(id, [question, followUp], [current.Answer, tomorrow.Answer]);
    }

    private static void AssertWeatherAnswer(Reply reply)
    {
        Assert.Contains("get_weather", reply.UsedTools);
        Assert.DoesNotContain("search_knowledge", reply.UsedTools);
        Assert.Empty(reply.Sources); // Weather attribution belongs in the answer, not document citations.
        Assert.Contains("אילת", reply.Answer);
        Assert.Matches(@"\d+(?:[.,]\d+)?\s*(?:מעלות|°)", reply.Answer);
        Assert.Contains("Open-Meteo", reply.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("UTC", reply.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.Matches(@"\b\d{1,2}:\d{2}\b", reply.Answer);
        Assert.DoesNotContain("טמפרטורת מראה", reply.Answer);
    }

    private static bool ContainsDate(string answer, DateTime date)
    {
        var hebrewMonth = date.ToString("MMMM", CultureInfo.GetCultureInfo("he-IL"));
        var numeric = $@"(?:{date:yyyy-MM-dd}|0?{date.Day}[./-]0?{date.Month}[./-]{date.Year})";
        var words = $@"{date.Day}\s+ב?{Regex.Escape(hebrewMonth)}\s+{date.Year}";
        return Regex.IsMatch(answer, $@"(?<!\d)(?:{numeric}|{words})(?!\d)");
    }
}
