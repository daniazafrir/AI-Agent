namespace Agent.Api.Features.Knowledge.Extraction;

public interface IDocumentTextExtractor
{
    bool CanHandle(string extension);

    Task<string> ExtractAsync(
        Stream stream,
        CancellationToken cancellationToken = default);
}