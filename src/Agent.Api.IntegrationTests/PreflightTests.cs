using System.Security.Authentication;
using System.Text.Json;
using Mcp.Tools.Server.Features.Rag;
using Mcp.Tools.Server.Tools;
using Agent.Api.IntegrationTests.Rag;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Agent.Api.IntegrationTests;

[Trait("Category", "Preflight")]
public sealed class PreflightTests
{
    [Fact]
    public async Task RequiredServices_AreReady()
    {
        var failures = new List<string>();
        await Check("Agent.Api readiness (including MCP and PostgreSQL)", async token =>
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("testsettings.json")
                .AddEnvironmentVariables().Build();
            var address = configuration["AGENT_E2E_BASE_URL"];
            if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "http" && uri.Scheme != "https"))
                return "Set AGENT_E2E_BASE_URL to the running Agent.Api HTTP(S) origin.";
            using var http = new HttpClient { BaseAddress = uri };
            using var response = await http.GetAsync("/health/ready", token);
            using var report = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
            var unavailable = report.RootElement.GetProperty("checks").EnumerateArray()
                .Where(x => x.GetProperty("status").GetString() != "Healthy")
                .Select(x => x.GetProperty("name").GetString()).ToArray();
            if (unavailable.Length > 0)
                return "Unhealthy dependencies: " + string.Join(", ", unavailable) +
                    ". Start/configure these services and check /health/ready.";
            if (!response.IsSuccessStatusCode)
                return $"Readiness returned HTTP {(int)response.StatusCode}. Check /health/ready and start the reported services.";
            if (report.RootElement.GetProperty("status").GetString() != "Healthy")
                return "Agent.Api is not Healthy. Check /health/ready.";
            return null;
        });

        await Check("Direct RAG (OpenAI embeddings, Qdrant, PostgreSQL)", async token =>
        {
            using var fixture = new RagFixture();
            using var scope = fixture.CreateScope();
            var rag = scope.ServiceProvider.GetRequiredService<IRagService>();
            await rag.SearchAsync("integration preflight connectivity", topK: 1, cancellationToken: token);
            return null; // No corpus or relevance assertions: this is connectivity only.
        });

        await Check("Open-Meteo (geocoding and forecast)", async token =>
        {
            using var http = new HttpClient();
            var result = JsonSerializer.SerializeToElement(
                await new WeatherTools(http).GetWeather("Eilat", "IL", token));
            return result.GetProperty("success").GetBoolean() ? null :
                "Weather probe failed: " + result.GetProperty("error").GetString() +
                ". Check outbound HTTPS connectivity to Open-Meteo.";
        });

        Assert.True(failures.Count == 0,
            "Integration preflight failed. Fix these dependencies before running acceptance scenarios:\n" +
            string.Join("\n", failures));

        async Task Check(string name, Func<CancellationToken, Task<string?>> check)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try
            {
                var issue = await check(timeout.Token);
                if (issue is not null) failures.Add(name + ": " + issue);
            }
            catch (Exception exception)
            {
                var chain = new List<Exception>();
                for (Exception? item = exception; item is not null; item = item.InnerException)
                    chain.Add(item);
                // Avoid dumping configuration, API keys, or connection strings.
                var reason = chain.Any(x => x is AuthenticationException)
                    ? "TLS/SSL authentication failed in this test process. Check its network/certificate environment; do not disable certificate validation."
                    : exception is OperationCanceledException
                        ? "Timed out after 30 seconds."
                        : "Connection or configuration failed (" + exception.GetType().Name +
                          "). Check service availability and test-process credentials.";
                failures.Add(name + ": " + reason);
            }
        }
    }
}
