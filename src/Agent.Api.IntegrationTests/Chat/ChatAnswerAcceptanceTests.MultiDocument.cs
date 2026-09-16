using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Agent.Api.IntegrationTests.Chat;

public sealed partial class ChatAnswerAcceptanceTests
{
    public static IEnumerable<object[]> MultiDocumentCases()
    {
        foreach (var streaming in new[] { false, true })
            foreach (var scenario in new[] { "meals", "submission", "payment", "mileage", "vacation" })
                yield return new object[] { streaming, scenario };
    }

    [Theory]
    [MemberData(nameof(MultiDocumentCases))]
    [Trait("Scenario", "MultiDocument")]
    public async Task MultiDocument_SearchesAndCitesSupportingDocument(bool streaming, string scenario)
    {
        var (query, patterns) = scenario switch
        {
            "meals" => (
                "What is the daily meal reimbursement limit for business travel?",
                new[] {
                    @"\b(?:45|forty[- ]five)\b",
                    @"(?:\bUSD\b|\bdollars?\b|\$)",
                    @"\b(?:per|each|a)\s+person\b",
                    @"\b(?:per|each|a)\s+(?:travel\s+)?day\b"
                }),
            "submission" => (
                "When must I submit a travel reimbursement claim?",
                new[] {
                    @"\b(?:14|fourteen)\s+calendar\s+days\b",
                    @"\bafter\b[^.!?\r\n]{0,100}\b(?:trip|travel)\b[^.!?\r\n]{0,60}\b(?:ends?|ended|completion|completed)\b"
                }),
            "payment" => (
                "When are approved travel claims paid?",
                new[] {
                    @"\b(?:10|ten)\s+business\s+days\b",
                    @"\bafter\b[^.!?\r\n]{0,100}\b(?:finance|financial)\s+approval\b"
                }),
            "mileage" => (
                "What is the mileage reimbursement rate?",
                new[] {
                    @"\bmileage\b",
                    @"(?:\bnot\s+(?:be\s+)?(?:defined|specified|provided|available|found|included)\b|\bdoes\s+not\s+(?:define|specify|provide|include|contain)\b|\bno\s+(?:defined\s+|specified\s+)?mileage\s+reimbursement\s+rate\b)"
                }),
            "vacation" => (
                "How many vacation days do employees receive?",
                new[] {
                    @"\b(?:19|nineteen)\s+(?:(?:annual|paid|vacation|working|business|calendar)\s+)*days?\b",
                    @"\bafter\b[^.!?\r\n]{0,100}\b(?:(?:one|1|first)\s+year|(?:12|twelve)\s+months)\b"
                }),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };

        var reply = await SendAsync(query, streaming);
        Assert.Contains("search_knowledge", reply.UsedTools);
        Assert.DoesNotContain("calculate", reply.UsedTools);
        foreach (var pattern in patterns)
            Assert.True(Regex.IsMatch(reply.Answer, pattern, RegexOptions.IgnoreCase),
                $"Scenario: {scenario}; missing pattern: {pattern}\nAnswer: {reply.Answer}");

        if (scenario == "mileage")
        {
            Assert.DoesNotMatch(@"\d", reply.Answer);
            Assert.DoesNotMatch(@"(?i)(?:[$€£]|\b(?:USD|dollars?|cents?)\b)", reply.Answer);
        }

        var documentName = RequiredEnvironment(scenario == "vacation"
            ? "RAG_E2E_DOCUMENT_NAME" : "RAG_E2E_TRAVEL_DOCUMENT_NAME");
        var sources = reply.Sources.Where(source =>
            source.DocumentId != Guid.Empty && source.ChunkIndex >= 0 &&
            string.Equals(source.DocumentName, documentName, StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.True(sources.Count > 0,
            $"Expected source {documentName}; received: {string.Join(", ", reply.Sources.Select(s => s.DocumentName))}");
        Assert.Equal(documentName, reply.Sources[0].DocumentName,
            ignoreCase: true);

        // Validate the actual cited chunks, not just the source file name.
        using var client = new HttpClient
        {
            BaseAddress = new Uri(RequiredEnvironment("AGENT_E2E_BASE_URL")),
            Timeout = TimeSpan.FromSeconds(15)
        };
        var citedText = new List<string>();
        foreach (var source in sources.DistinctBy(s => (s.DocumentId, s.ChunkIndex)))
        {
            var chunk = await client.GetFromJsonAsync<CitedChunk>(
                $"/api/documents/{source.DocumentId}/chunks/{source.ChunkIndex}", JsonOptions);
            Assert.NotNull(chunk);
            Assert.Equal(source.DocumentId, chunk.DocumentId);
            Assert.Equal(source.ChunkIndex, chunk.ChunkIndex);
            Assert.Equal(documentName, chunk.DocumentName, ignoreCase: true);
            citedText.Add(chunk.Content);
        }
        var evidence = string.Join("\n", citedText);
        foreach (var pattern in patterns)
            Assert.True(Regex.IsMatch(evidence, pattern, RegexOptions.IgnoreCase),
                $"The cited chunks in {documentName} do not support pattern: {pattern}");
    }

    public sealed class CitedChunk
    {
        public Guid DocumentId { get; set; }
        public string DocumentName { get; set; } = "";
        public int ChunkIndex { get; set; }
        public string Content { get; set; } = "";
    }
}

