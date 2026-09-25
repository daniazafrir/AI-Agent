using Agent.Api.Tools;
using Xunit;

namespace Agent.Api.Tests.Tools;

public sealed class KnowledgeContextBuilderTests
{
    [Fact]
    public void TracksActualIncludedAndOmittedChunks()
    {
        var result = KnowledgeContextBuilder.Build("""{"matches":[{"documentId":"a","documentName":"Handbook","chunkIndex":0,"content":"19 days"},{"documentId":"b","documentName":"Empty","chunkIndex":2,"content":" "}]}""");
        Assert.Contains("Source: Handbook", result.Content);
        Assert.Contains("19 days", result.Content);
        Assert.DoesNotContain("Empty", result.Content);
        Assert.Equal(2, result.Analytics!.ReturnedChunks);
        Assert.Equal(1, result.Analytics.IncludedChunks);
        Assert.Equal(1, result.Analytics.OmittedChunks);
        Assert.Equal(result.Content.Length, result.Analytics.ContextCharacters);
        Assert.Equal(0, result.Analytics.Chunks[0].ChunkIndex);
        Assert.Equal("empty_content", result.Analytics.Chunks[1].Reason);
    }

    [Fact]
    public void EmptyContentFallbackIsRecordedAsRawPayloadInclusion()
    {
        const string raw = """{"matches":[{"DocumentName":"Empty","ChunkIndex":3,"Content":""}]}""";
        var result = KnowledgeContextBuilder.Build(raw);
        Assert.Equal(raw, result.Content);
        Assert.Equal(1, result.Analytics!.IncludedChunks);
        Assert.Equal("raw_payload_fallback", result.Analytics.Chunks[0].Reason);
        Assert.Equal(3, result.Analytics.Chunks[0].ChunkIndex);
    }

    [Fact]
    public void NoResultsContainsNoContextChunks()
    {
        var result = KnowledgeContextBuilder.Build("""{"success":true,"matches":[]}""");
        Assert.Equal(0, result.Analytics!.ReturnedChunks);
        Assert.Equal(0, result.Analytics.IncludedChunks);
        Assert.Empty(result.Analytics.Chunks);
        Assert.True(result.Analytics.ContextCharacters > 0);
    }

    [Fact]
    public void ReadsPascalCaseFieldsAndPreservesDuplicateEntries()
    {
        var result = KnowledgeContextBuilder.Build("""{"matches":[{"DocumentId":"a","DocumentName":"Doc","ChunkIndex":1,"Content":"text"},{"DocumentId":"a","DocumentName":"Doc","ChunkIndex":1,"Content":"text"}]}""");
        Assert.Equal(2, result.Analytics!.IncludedChunks);
        Assert.All(result.Analytics.Chunks, chunk => Assert.Equal("a", chunk.DocumentId));
    }
}
