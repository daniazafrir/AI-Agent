namespace Agent.Api.Features.Knowledge.Extraction;

public sealed class TxtDocumentTextExtractor
    : IDocumentTextExtractor
{
    public bool CanHandle(string extension)
    {
        return string.Equals(
            extension,
            ".txt",
            StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string> ExtractAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        using var reader =
            new StreamReader(
                stream,
                leaveOpen: true);

        return await reader.ReadToEndAsync(
            cancellationToken);
    }
}