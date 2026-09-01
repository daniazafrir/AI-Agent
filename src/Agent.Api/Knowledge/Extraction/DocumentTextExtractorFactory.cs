namespace Agent.Api.Features.Knowledge.Extraction;

public sealed class DocumentTextExtractorFactory(
    IEnumerable<IDocumentTextExtractor> extractors)
{
    public IDocumentTextExtractor Get(
        string extension)
    {
        var extractor =
            extractors.FirstOrDefault(
                x => x.CanHandle(extension));

        if (extractor is null)
        {
            throw new NotSupportedException(
                $"File type '{extension}' is not supported.");
        }

        return extractor;
    }
}