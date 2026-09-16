using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Agent.Api.IntegrationTests.Chat;

public sealed partial class ChatAnswerAcceptanceTests
{
    [Fact]
    [Trait("Scenario", "DocumentLifecycle")]
    public async Task DocumentLifecycle_DuplicateDeleteAndReplacement_KeepStoresAndAnswersConsistent()
    {
        var token = Guid.NewGuid().ToString("N");
        var fileName = $"Lifecycle_{token}.txt";
        var policy = $"Lifecycle-{token}";
        var query = $"According to policy {policy}, what is the reimbursement limit in USD?";
        var version1 = $"Test policy {policy}\nThe reimbursement limit under policy {policy} is 73 USD per claim.";
        var version2 = $"Test policy {policy}\nThe reimbursement limit under policy {policy} is 91 USD per claim.";
        using var api = new HttpClient
        {
            BaseAddress = new Uri(RequiredEnvironment("AGENT_E2E_BASE_URL")),
            Timeout = TimeSpan.FromMinutes(2)
        };
        using var vectors = new HttpClient
        {
            BaseAddress = new Uri(RequiredEnvironment("RAG_E2E_QDRANT_URL")),
            Timeout = TimeSpan.FromSeconds(15)
        };
        var collection = Uri.EscapeDataString(RequiredEnvironment("RAG_E2E_QDRANT_COLLECTION"));

        async Task<long> VectorCount(Guid id)
        {
            using var response = await vectors.PostAsJsonAsync(
                $"/collections/{collection}/points/count",
                new { exact = true, filter = new { must = new[] {
                    new { key = "documentId", match = new { value = id.ToString() } }
                } } });
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return json.RootElement.GetProperty("result").GetProperty("count").GetInt64();
        }

        async Task<LifecycleDocument[]> List() =>
            await api.GetFromJsonAsync<LifecycleDocument[]>("/api/documents", JsonOptions)
                ?? throw new InvalidOperationException("Document listing was empty.");

        async Task<LifecycleUpload> Upload(string content)
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(content, Encoding.UTF8, "text/plain"), "file", fileName);
            using var response = await api.PostAsync("/api/documents", form);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<LifecycleUpload>(JsonOptions);
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotEqual(Guid.Empty, result.DocumentId);
            Assert.Equal(fileName, result.FileName);
            Assert.Equal(1, result.ChunkCount);
            return result;
        }

        async Task Delete(Guid id)
        {
            // Only delete the document carrying this run's unique file name.
            Assert.Contains(await List(), d => d.Id == id && d.FileName == fileName);
            using var response = await api.DeleteAsync($"/api/documents/{id}");
            response.EnsureSuccessStatusCode();
            Assert.True(await response.Content.ReadFromJsonAsync<bool>(JsonOptions));
        }

        async Task AssertDeleted(Guid id)
        {
            Assert.DoesNotContain(await List(), d => d.Id == id);
            using var response = await api.GetAsync($"/api/documents/{id}/chunks/0");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(0, await VectorCount(id));
        }

        async Task AssertAnswer(Guid id, string expected, string forbidden)
        {
            // Each request uses a fresh conversation so historical answers cannot
            // conceal a stale vector or document record.
            var reply = await SendAsync(query, streaming: true);
            Assert.Contains("search_knowledge", reply.UsedTools);
            Assert.Matches($@"\b{expected}\b", reply.Answer);
            Assert.DoesNotMatch($@"\b{forbidden}\b", reply.Answer);
            Assert.Contains(reply.Sources, source => source.DocumentId == id &&
                source.DocumentName == fileName && source.ChunkIndex == 0);
        }

        // Check read access before creating any test data.
        Assert.DoesNotContain(await List(), d => d.FileName == fileName);
        await VectorCount(Guid.Empty);
        try
        {
            var first = await Upload(version1);
            Assert.False(first.AlreadyExists);
            Assert.Equal(1, await VectorCount(first.DocumentId));
            var originalChunk = await api.GetFromJsonAsync<CitedChunk>(
                $"/api/documents/{first.DocumentId}/chunks/0", JsonOptions);
            Assert.NotNull(originalChunk);
            Assert.Contains("73 USD", originalChunk.Content);

            var duplicate = await Upload(version1);
            Assert.True(duplicate.AlreadyExists);
            Assert.Equal(first.DocumentId, duplicate.DocumentId);
            Assert.Single(await List(), d => d.FileName == fileName);
            Assert.Equal(1, await VectorCount(first.DocumentId));
            using (var extraChunk = await api.GetAsync($"/api/documents/{first.DocumentId}/chunks/1"))
                Assert.Equal(HttpStatusCode.NotFound, extraChunk.StatusCode);
            await AssertAnswer(first.DocumentId, "73", "91");

            await Delete(first.DocumentId);
            await AssertDeleted(first.DocumentId);
            var absent = await SendAsync(query, streaming: true);
            Assert.Contains("search_knowledge", absent.UsedTools);
            Assert.DoesNotContain(absent.Sources, source =>
                source.DocumentId == first.DocumentId || source.DocumentName == fileName);
            Assert.DoesNotMatch(@"\b73\b", absent.Answer);

            var replacement = await Upload(version2);
            Assert.False(replacement.AlreadyExists);
            Assert.NotEqual(first.DocumentId, replacement.DocumentId);
            Assert.Single(await List(), d => d.FileName == fileName);
            Assert.Equal(0, await VectorCount(first.DocumentId));
            Assert.Equal(1, await VectorCount(replacement.DocumentId));
            var updatedChunk = await api.GetFromJsonAsync<CitedChunk>(
                $"/api/documents/{replacement.DocumentId}/chunks/0", JsonOptions);
            Assert.NotNull(updatedChunk);
            Assert.Contains("91 USD", updatedChunk.Content);
            Assert.DoesNotContain("73 USD", updatedChunk.Content);
            await AssertAnswer(replacement.DocumentId, "91", "73");
        }
        finally
        {
            // Recover by this run's name even if upload succeeded server-side but
            // the response was lost. Never delete other documents or collections.
            foreach (var document in (await List()).Where(d => d.FileName == fileName))
            {
                await Delete(document.Id);
                await AssertDeleted(document.Id);
            }
        }
    }

    public sealed class LifecycleDocument
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = "";
    }

    public sealed class LifecycleUpload
    {
        public bool Success { get; set; }
        public bool AlreadyExists { get; set; }
        public Guid DocumentId { get; set; }
        public string FileName { get; set; } = "";
        public int ChunkCount { get; set; }
    }
}
