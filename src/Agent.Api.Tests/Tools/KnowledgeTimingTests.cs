using Mcp.Tools.Server.Features.Rag;
using Mcp.Tools.Server.Tools;
using Moq;
using System.Text.Json;
using Xunit;

namespace Agent.Api.Tests.Tools;

public sealed class KnowledgeTimingTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ToolSerializesTimingsAndPreservesMissingValues(bool measured)
    {
        var rag = new Mock<IRagService>();
        rag.Setup(x => x.SearchAsync("query", 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagSearchResult {
                EmbeddingTimeMs = measured ? 120 : null,
                VectorSearchTimeMs = measured ? 20 : null,
                KeywordSearchTimeMs = measured ? 30 : null,
                RankingTimeMs = measured ? 0 : null,
                SearchTimeMs = 155
            });
        using var json = JsonDocument.Parse(await KnowledgeTools.SearchKnowledgeAsync(rag.Object, "query"));
        Assert.Equal(155, json.RootElement.GetProperty("searchTimeMs").GetInt64());
        foreach (var (name, expected) in new[] { ("embeddingTimeMs", 120L), ("vectorSearchTimeMs", 20L), ("keywordSearchTimeMs", 30L), ("rankingTimeMs", 0L) })
        {
            var value = json.RootElement.GetProperty(name);
            if (measured) Assert.Equal(expected, value.GetInt64());
            else Assert.Equal(JsonValueKind.Null, value.ValueKind);
        }
    }
}
