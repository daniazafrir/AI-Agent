using Microsoft.Extensions.DependencyInjection;
using Mcp.Tools.Server.Features.Rag;
using Xunit;

namespace Agent.Api.IntegrationTests.Rag;

[Trait("Category", "Integration")]
public sealed class RagAcceptanceTests : IClassFixture<RagFixture>, IDisposable
{
    private readonly IRagService _ragService;
    private readonly IServiceScope _scope;
    public void Dispose() => _scope.Dispose();

    public RagAcceptanceTests(
        RagFixture fixture)
    {
        _scope = fixture.CreateScope();
        _ragService = _scope.ServiceProvider.GetRequiredService<IRagService>();
    }

    [Theory]
    [InlineData("vacation days for employees")]
    [InlineData("How many vacation days do employees receive?")]
    [InlineData("What is the vacation allowance for employees?")]
    public async Task SearchAsync_DirectVacationQuery_ReturnsVacationInformation(string query)
    {
        // Act
        var result =
            await _ragService.SearchAsync(
                query,
                topK: 5);

        // Assert
        Assert.NotNull(result);

        Assert.True(
            result.RawVectorResults > 0);

        Assert.True(
            result.RelevantVectorResults > 0);

        Assert.True(
            result.MergedResults > 0);

        Assert.Equal(
            0.45,
            result.MinimumVectorScore,
            precision: 2);

        Assert.Contains(
            result.Matches,
            match =>
                match.Content.Contains(
                    "19",
                    StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SearchAsync_SemanticVacationQuery_ReturnsVacationInformation()
    {
        // Arrange
        const string query =
            "annual time off for staff";

        // Act
        var result =
            await _ragService.SearchAsync(
                query,
                topK: 5);

        // Assert
        Assert.NotNull(result);

        Assert.True(
            result.RawVectorResults > 0);

        Assert.True(
            result.RelevantVectorResults >= 1);

        /*
         * This is the important semantic RAG test:
         * PostgreSQL keyword search should not be required
         * for this query.
         */
        Assert.Equal(
            0,
            result.KeywordResults);

        Assert.True(
            result.MergedResults >= 1);

        Assert.Contains(
            result.Matches,
            match =>
                match.Content.Contains(
                    "19",
                    StringComparison.OrdinalIgnoreCase));

        Assert.Equal(
            0.45,
            result.MinimumVectorScore,
            precision: 2);
    }

    [Theory]
    [InlineData("What is the CEO's phone number?")]
    [InlineData("How can I contact the CEO by phone?")]
    [InlineData("What is the chief executive's telephone number?")]
    public async Task SearchAsync_UnrelatedCeoPhoneQuery_ReturnsNoResults(string query)
    {
        // Act
        var result =
            await _ragService.SearchAsync(
                query,
                topK: 5);

        // Assert
        Assert.NotNull(result);

        Assert.True(
            result.RawVectorResults > 0);

        Assert.Equal(
            0,
            result.RelevantVectorResults);

        Assert.Equal(
            0,
            result.KeywordResults);

        Assert.Equal(
            0,
            result.MergedResults);

        Assert.Empty(
            result.Matches);

        Assert.Equal(
            0.45,
            result.MinimumVectorScore,
            precision: 2);
    }

    [Theory]
    [InlineData("How much annual leave are staff entitled to?")]
    [InlineData("How many days off does a worker get each year?")]
    [InlineData("What is the yearly time off entitlement for staff?")]
    public async Task SearchAsync_VacationParaphrase_ReturnsVacationInformation(string query)
    {
        var result = await _ragService.SearchAsync(query, topK: 5);

        Assert.True(result.RawVectorResults > 0, $"No vector candidates for: {query}");
        Assert.True(result.RelevantVectorResults > 0, $"No relevant vectors for: {query}");
        Assert.True(result.MergedResults > 0, $"No merged results for: {query}");
        Assert.Equal(0.45, result.MinimumVectorScore, precision: 2);
        Assert.Contains(result.Matches, match =>
            System.Text.RegularExpressions.Regex.IsMatch(match.Content, @"\b19\b"));

        // These new phrasings can also match keywords. Only the separately
        // calibrated "annual time off for staff" case requires zero keyword hits.
    }
}

