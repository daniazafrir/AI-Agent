using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

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

        ArgumentNullException.ThrowIfNull(stream);

        using var memoryStream =
            new MemoryStream();

        stream.CopyTo(memoryStream);

        var bytes =
            memoryStream.ToArray();

        using var document =
            PdfDocument.Open(bytes);

        var builder =
            new StringBuilder();

        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pageText =
                ContentOrderTextExtractor
                    .GetText(page);

            if (string.IsNullOrWhiteSpace(pageText))
            {
                continue;
            }

            pageText =
                NormalizePageText(
                    pageText);

            if (builder.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine();
            }

            builder.Append(pageText);
        }

        return Task.FromResult(
            builder
                .ToString()
                .Trim());
    }

    private static string NormalizePageText(
        string text)
    {
        var normalized =
            text.Replace(
                "\r\n",
                "\n")
            .Replace(
                '\r',
                '\n');

        /*
         * מתקנים מילים שנדבקו בזמן חילוץ PDF:
         *
         * WorkEmployees
         * ->
         * Work Employees
         */
        normalized =
            MissingSpaceRegex.Replace(
                normalized,
                " ");

        /*
         * מנקים tabs ורווחים כפולים,
         * אבל שומרים line breaks.
         */
        normalized =
            HorizontalWhitespaceRegex.Replace(
                normalized,
                " ");

        /*
         * מנקים רווח בתחילת/סוף כל שורה.
         */
        var lines =
            normalized
                .Split('\n')
                .Select(x => x.Trim())
                .ToList();

        normalized =
            string.Join(
                "\n",
                lines);

        /*
         * יותר משתי שורות ריקות
         * הופכות לשורה ריקה אחת.
         */
        normalized =
            ExcessiveNewLinesRegex.Replace(
                normalized,
                "\n\n");

        return normalized.Trim();
    }

    private static readonly Regex MissingSpaceRegex =
        new(
            @"(?<=[a-z])(?=[A-Z])",
            RegexOptions.Compiled);

    private static readonly Regex HorizontalWhitespaceRegex =
        new(
            @"[ \t]+",
            RegexOptions.Compiled);

    private static readonly Regex ExcessiveNewLinesRegex =
        new(
            @"\n{3,}",
            RegexOptions.Compiled);
}