using System.Text;
using UglyToad.PdfPig;

namespace Agent.Api.Features.Knowledge.Extraction;

public sealed class PdfDocumentTextExtractor
    : IDocumentTextExtractor
{
    public bool CanHandle(string extension)
    {
        return string.Equals(
            extension,
            ".pdf",
            StringComparison.OrdinalIgnoreCase);
    }

    public Task<string> ExtractAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var memoryStream =
            new MemoryStream();

        stream.CopyTo(memoryStream);

        memoryStream.Position = 0;

        using var document =
            PdfDocument.Open(
                memoryStream.ToArray());

        var builder =
            new StringBuilder();

        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();

            builder.AppendLine(
                page.Text);
        }

        return Task.FromResult(
            builder.ToString());
    }
}