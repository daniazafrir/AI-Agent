using System.Diagnostics;
using Xunit;

namespace Agent.Api.IntegrationTests.Chat;

public sealed partial class ChatAnswerAcceptanceTests
{
    [Fact]
    [Trait("Scenario", "Performance")]
    public async Task VacationAnswer_ReportsRequestLatencyWithinOperationalBudget()
    {
        const string query = "How many vacation days do employees receive?";

        var normalClock = Stopwatch.StartNew();
        var normal = await SendAsync(query, streaming: false);
        normalClock.Stop();

        var streamClock = Stopwatch.StartNew();
        var streamed = await SendAsync(query, streaming: true);
        streamClock.Stop();

        Assert.Contains("search_knowledge", normal.UsedTools);
        Assert.Contains("search_knowledge", streamed.UsedTools);
        Assert.False(string.IsNullOrWhiteSpace(normal.Answer));
        Assert.False(string.IsNullOrWhiteSpace(streamed.Answer));

        // This is an operational guardrail, not a benchmark. It catches a
        // stuck request while allowing normal model/network variance.
        Assert.True(normalClock.Elapsed < TimeSpan.FromSeconds(120),
            $"Normal response took {normalClock.Elapsed.TotalSeconds:F1}s.");
        Assert.True(streamClock.Elapsed < TimeSpan.FromSeconds(120),
            $"SSE response took {streamClock.Elapsed.TotalSeconds:F1}s.");

        Console.WriteLine(
            $"Performance baseline: normal={normalClock.ElapsedMilliseconds}ms, " +
            $"sse={streamClock.ElapsedMilliseconds}ms");
    }
}
