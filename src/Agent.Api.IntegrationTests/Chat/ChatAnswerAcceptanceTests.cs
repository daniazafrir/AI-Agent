using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Agent.Api.IntegrationTests.Chat;

[Trait("Category", "EndToEnd")]
public sealed partial class ChatAnswerAcceptanceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData(false, "How many vacation days do employees receive?")]
    [InlineData(true, "How many vacation days do employees receive?")]
    [InlineData(false, "How much annual time off are staff entitled to?")]
    [InlineData(true, "How much annual time off are staff entitled to?")]
    public async Task VacationQuestion_Returns19DaysAndVacationSource(bool streaming, string query)
    {
        var documentName = RequiredEnvironment("RAG_E2E_DOCUMENT_NAME");
        var result = await SendAsync(query, streaming);

        Assert.Contains("search_knowledge", result.UsedTools);
        Assert.DoesNotContain("calculate", result.UsedTools);
        Assert.Matches(@"(?i)\b(?:19|nineteen)\s+(?:(?:annual|paid|vacation|working|business|calendar)\s+)*days?\b",
            result.Answer);
        Assert.Contains(result.Sources, source =>
            source.DocumentId != Guid.Empty &&
            source.ChunkIndex >= 0 &&
            string.Equals(documentName, source.DocumentName, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnknownCeoPhone_ReportsMissingInformationWithoutSources(bool streaming)
    {
        var result = await SendAsync("What is the CEO's phone number?", streaming);

        Assert.Contains("search_knowledge", result.UsedTools);
        Assert.DoesNotContain("calculate", result.UsedTools);
        Assert.Empty(result.Sources);
        Assert.Matches(
            @"(?i)(not (?:be )?(?:found|available|provided|listed)|could(?:n't| not) find|cannot find|can['’]t find|do(?:n't| not) have|does(?:n't| not) (?:contain|include|provide)|no (?:information|phone|telephone|contact)|unable to find)",
            result.Answer);
        Assert.False(Regex.IsMatch(result.Answer, @"(?:\d[\s()+.-]*){7,}"),
            "The no-answer response must not contain a phone-like number.");
        Assert.DoesNotContain("@", result.Answer);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Conversation_FollowUpAndTopicChange_RemainGrounded(bool streaming)
    {
        var id = Guid.NewGuid();
        const string firstQuery = "How many vacation days do employees receive?";
        const string followUp = "And when does that entitlement begin?";
        const string topicChange = "What is the CEO's phone number?";
        var first = await SendAsync(firstQuery, streaming, id);
        Assert.Matches(@"(?i)\b(?:19|nineteen)\b", first.Answer);
        AssertVacationSource(first);

        var second = await SendAsync(followUp, streaming, id);
        Assert.True(Regex.IsMatch(second.Answer,
            @"(?i)\b(?:(?:one|1|first)\s+(?:full\s+)?year|(?:12|twelve)\s+months)\b"),
            "Expected a one-year eligibility period. Full answer: " + second.Answer);
        Assert.Contains("search_knowledge", second.UsedTools);
        AssertVacationSource(second);
        Assert.DoesNotContain("calculate", second.UsedTools);

        var third = await SendAsync(topicChange, streaming, id);
        Assert.Contains("search_knowledge", third.UsedTools);
        Assert.Empty(third.Sources);
        Assert.Matches(@"(?i)(not (?:be )?(?:found|available|provided|listed)|could(?:n't| not) find|cannot find|do(?:n't| not) have|no (?:information|phone|contact))", third.Answer);
        Assert.DoesNotMatch(@"(?:\d[\s()+.-]*){7,}", third.Answer);
        Assert.DoesNotContain("@", third.Answer);
        Assert.DoesNotContain("calculate", third.UsedTools);
        await AssertHistoryAsync(id, [firstQuery, followUp, topicChange],
            [first.Answer, second.Answer, third.Answer]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Conversation_NewId_DoesNotInheritOtherConversation(bool streaming)
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var marker = "marker-" + Guid.NewGuid().ToString("N");
        var introduction = $"For this conversation, remember my test marker: {marker}.";
        const string recall = "What test marker did I give you earlier in this conversation?";
        var introduced = await SendAsync(introduction, streaming, firstId);
        var remembered = await SendAsync(recall, streaming, firstId);
        Assert.Contains(marker, remembered.Answer);

        var fresh = await SendAsync(recall, streaming, secondId);
        Assert.DoesNotContain(marker, fresh.Answer);
        Assert.Empty(fresh.Sources);
        await AssertHistoryAsync(firstId, [introduction, recall],
            [introduced.Answer, remembered.Answer]);
        await AssertHistoryAsync(secondId, [recall], [fresh.Answer]);
    }

    private static void AssertVacationSource(Reply reply)
    {
        var name = RequiredEnvironment("RAG_E2E_DOCUMENT_NAME");
        Assert.Contains(reply.Sources, source =>
            source.DocumentId != Guid.Empty && source.ChunkIndex >= 0 &&
            string.Equals(source.DocumentName, name, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task AssertHistoryAsync(Guid id, string[] users, string[] assistants)
    {
        using var client = new HttpClient
        {
            BaseAddress = new Uri(RequiredEnvironment("AGENT_E2E_BASE_URL")),
            Timeout = TimeSpan.FromSeconds(15)
        };
        var history = await client.GetFromJsonAsync<History>($"/api/conversations/{id}", JsonOptions);
        Assert.NotNull(history);
        Assert.Equal(id, history.ConversationId);
        Assert.Equal(users.Length * 2, history.Messages.Length);
        for (var i = 0; i < users.Length; i++)
        {
            Assert.Equal("user", history.Messages[i * 2].Role.ToLowerInvariant());
            Assert.Equal(users[i], history.Messages[i * 2].Content);
            Assert.Equal("assistant", history.Messages[i * 2 + 1].Role.ToLowerInvariant());
            Assert.Equal(assistants[i], history.Messages[i * 2 + 1].Content);
        }
    }

    public sealed class History
    {
        public Guid ConversationId { get; set; }
        public HistoryMessage[] Messages { get; set; } = [];
    }

    public sealed class HistoryMessage
    {
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
    }
    private static async Task<Reply> SendAsync(string query, bool streaming, Guid? existingConversationId = null)
    {
        var baseUrl = RequiredEnvironment("AGENT_E2E_BASE_URL");
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
            throw new InvalidOperationException("AGENT_E2E_BASE_URL must be an HTTP(S) URL.");

        using var client = new HttpClient { BaseAddress = uri, Timeout = TimeSpan.FromMinutes(2) };
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        // A unique conversation prevents earlier test answers from affecting retrieval.
        var conversationId = existingConversationId ?? Guid.NewGuid();
        using var request = new HttpRequestMessage(HttpMethod.Post,
            streaming ? "/api/chat/stream" : "/api/chat")
        {
            Content = JsonContent.Create(new { message = query, conversationId })
        };
        using var response = await client.SendAsync(request,
            HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        Assert.True(response.IsSuccessStatusCode, $"Chat returned HTTP {(int)response.StatusCode}.");

        if (!streaming)
        {
            var reply = await response.Content.ReadFromJsonAsync<Reply>(JsonOptions, timeout.Token);
            Assert.NotNull(reply);
            Assert.Equal(conversationId, reply.ConversationId);
            Assert.False(string.IsNullOrWhiteSpace(reply.Answer));
            return reply;
        }

        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
        using var reader = new StreamReader(stream);
        var answer = new StringBuilder();
        var data = new StringBuilder();
        Reply? completed = null;
        var sawConversation = false;

        void Dispatch()
        {
            if (data.Length == 0) return;
            var item = JsonSerializer.Deserialize<StreamEvent>(data.ToString(), JsonOptions);
            data.Clear();
            Assert.NotNull(item);
            Assert.NotEqual("error", item.Type);
            if (item.Type == "conversation")
            {
                Assert.Equal(conversationId, item.ConversationId);
                sawConversation = true;
            }
            if (item.Type == "content") answer.Append(item.Content);
            if (item.Type == "completed")
            {
                Assert.Null(completed);
                completed = new Reply
                {
                    ConversationId = conversationId,
                    UsedTools = item.UsedTools ?? [],
                    Sources = item.Sources ?? []
                };
            }
        }

        while (await reader.ReadLineAsync(timeout.Token) is { } line)
        {
            if (line.Length == 0) Dispatch();
            else if (line.StartsWith("data:", StringComparison.Ordinal))
            {
                if (data.Length > 0) data.AppendLine();
                data.Append(line[5..].TrimStart(' '));
            }
        }
        Dispatch();
        Assert.True(sawConversation, "Missing conversation event.");
        Assert.NotNull(completed);
        Assert.False(string.IsNullOrWhiteSpace(answer.ToString()), "Missing streamed answer.");
        completed.Answer = answer.ToString();
        return completed;
    }

    private static string RequiredEnvironment(string name)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("testsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();
        return !string.IsNullOrWhiteSpace(configuration[name])
            ? configuration[name]!
            : throw new InvalidOperationException($"Set {name} in testsettings.json or the environment.");
    }
    public sealed class Reply
    {
        public Guid ConversationId { get; set; }
        public string Answer { get; set; } = "";
        public string[] UsedTools { get; set; } = [];
        public Source[] Sources { get; set; } = [];
    }

    public sealed class Source
    {
        public Guid DocumentId { get; set; }
        public string DocumentName { get; set; } = "";
        public int ChunkIndex { get; set; }
    }

    public sealed class StreamEvent
    {
        public string Type { get; set; } = "";
        public string? Content { get; set; }
        public Guid? ConversationId { get; set; }
        public string[]? UsedTools { get; set; }
        public Source[]? Sources { get; set; }
    }
}
