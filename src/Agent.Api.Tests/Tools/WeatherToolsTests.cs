using System.Net;
using System.Text.Json;
using Agent.Api.Mcp;
using Agent.Api.Tools;
using Mcp.Tools.Server.Tools;
using Moq;
using OpenAI.Chat;

namespace Agent.Api.Tests.Tools;

public class WeatherToolsTests
{
    [Fact]
    public async Task Weather_ReturnsProviderValuesAndUnits()
    {
        var handler = new FakeHandler(
            """{"results":[{"name":"Eilat","latitude":29.55,"longitude":34.95,"country":"Israel"}]}""",
            """{"timezone":"Asia/Jerusalem","current":{"temperature_2m":31,"time":"2026-09-23T12:00"},"current_units":{"temperature_2m":"°C"},"daily":{"time":["2026-09-23"],"temperature_2m_max":[33]},"daily_units":{"temperature_2m_max":"°C"}}""");
        using var http = new HttpClient(handler);
        var result = JsonSerializer.SerializeToElement(await new WeatherTools(http).GetWeather("Eilat", "IL"));
        Assert.True(result.GetProperty("success").GetBoolean());
        Assert.Equal(31, result.GetProperty("current").GetProperty("temperature_2m").GetInt32());
        Assert.Equal("Open-Meteo", result.GetProperty("source").GetString());
        Assert.Contains("countryCode=IL", handler.Urls[0]);
        Assert.Contains("latitude=29.55", handler.Urls[1]);
        Assert.Contains("forecast_days=3", handler.Urls[1]);
    }

    [Theory]
    [InlineData("""{"results":[]}""", "location_not_found")]
    [InlineData("""{"results":[{"name":"A"},{"name":"B"}]}""", "ambiguous_location")]
    [InlineData("invalid json", "weather_unavailable")]
    public async Task InvalidOrAmbiguousResult_DoesNotRequestForecast(string body, string error)
    {
        var handler = new FakeHandler(body);
        using var http = new HttpClient(handler);
        var result = JsonSerializer.SerializeToElement(await new WeatherTools(http).GetWeather("City"));
        Assert.False(result.GetProperty("success").GetBoolean());
        Assert.Equal(error, result.GetProperty("error").GetString());
        Assert.Single(handler.Urls);
    }

    [Fact]
    public async Task HttpFailure_IsReportedWithoutWeather()
    {
        using var http = new HttpClient(new FakeHandler("{}") { Status = HttpStatusCode.ServiceUnavailable });
        var result = JsonSerializer.SerializeToElement(await new WeatherTools(http).GetWeather("Eilat"));
        Assert.Equal("weather_unavailable", result.GetProperty("error").GetString());
        Assert.False(result.TryGetProperty("current", out _));
    }

    [Fact]
    public async Task MissingLocation_MakesNoRequest_AndCancellationPropagates()
    {
        var handler = new FakeHandler();
        using var http = new HttpClient(handler);
        var tool = new WeatherTools(http);
        var result = JsonSerializer.SerializeToElement(await tool.GetWeather(""));
        Assert.Equal("missing_location", result.GetProperty("error").GetString());
        using var token = new CancellationTokenSource();
        token.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => tool.GetWeather("Eilat", cancellationToken: token.Token));
        Assert.Empty(handler.Urls);
    }

    [Theory]
    [InlineData("מה המזג באילת?", true)]
    [InlineData("What is the weather in Eilat?", true)]
    [InlineData("מה התחזית למחר?", true)]
    [InlineData("What is MCP?", false)]
    [InlineData("How many vacation days do employees receive?", false)]
    public void Router_ExposesWeatherForWeatherQuestions(string query, bool expected)
    {
        var registry = new Mock<IMcpToolRegistry>();
        registry.SetupGet(x => x.Definitions).Returns(new[] { ChatTool.CreateFunctionTool("get_weather") });
        var selected = new ToolRouter(registry.Object).SelectTools(new[] { new UserChatMessage(query) });
        Assert.Equal(expected, selected.Any(x => x.FunctionName == "get_weather"));
    }

    [Theory]
    [InlineData("כן", true)]
    [InlineData("yes", true)]
    [InlineData("אילת בישראל", true)]
    [InlineData("ומה מחר?", true)]
    [InlineData("What about tomorrow?", true)]
    [InlineData("ומחרתיים?", true)]
    [InlineData("What is MCP?", false)]
    public void Router_KeepsWeatherAfterClarification(string reply, bool expected)
    {
        var registry = new Mock<IMcpToolRegistry>();
        registry.SetupGet(x => x.Definitions).Returns(new[] { ChatTool.CreateFunctionTool("get_weather") });
        ChatMessage[] messages = [
            new UserChatMessage("מה המזג באילת?"),
            new AssistantChatMessage("התכוונת לאילת בישראל?"),
            new UserChatMessage(reply),
            new AssistantChatMessage("רגע אחד")
        ];
        Assert.Equal(expected, new ToolRouter(registry.Object).SelectTools(messages)
            .Any(x => x.FunctionName == "get_weather"));
    }

    [Fact]
    public async Task UniqueExactCity_DoesNotRequireClarificationForFuzzyMatches()
    {
        var handler = new FakeHandler(
            """{"results":[{"name":"Eilat","latitude":29.55,"longitude":34.95},{"name":"Eilat Airport","latitude":29.6,"longitude":34.9}]}""",
            """{"timezone":"Asia/Jerusalem","current":{},"current_units":{},"daily":{},"daily_units":{}}""");
        using var http = new HttpClient(handler);
        var result = JsonSerializer.SerializeToElement(await new WeatherTools(http).GetWeather("Eilat", "IL"));
        Assert.True(result.GetProperty("success").GetBoolean());
        Assert.Equal(2, handler.Urls.Count);
    }

    [Theory]
    [InlineData("מה המזג אוויר באילת?", true)]
    [InlineData("What is MCP?", false)]
    public void TomorrowFollowUp_UsesUserContextWithoutWeatherWordsInAssistantAnswer(string question, bool expected)
    {
        var registry = new Mock<IMcpToolRegistry>();
        registry.SetupGet(x => x.Definitions).Returns(new[] { ChatTool.CreateFunctionTool("get_weather") });
        ChatMessage[] messages = [
            new UserChatMessage(question),
            new AssistantChatMessage("35.7 °C"),
            new UserChatMessage("ומה מחר?")
        ];
        Assert.Equal(expected, new ToolRouter(registry.Object).SelectTools(messages)
            .Any(x => x.FunctionName == "get_weather"));
    }

    private sealed class FakeHandler(params string[] bodies) : HttpMessageHandler
    {
        public List<string> Urls { get; } = [];
        public HttpStatusCode Status { get; init; } = HttpStatusCode.OK;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Urls.Add(request.RequestUri!.ToString());
            return Task.FromResult(new HttpResponseMessage(Status) { Content = new StringContent(bodies[Urls.Count - 1]) });
        }
    }
}
