using System.Text;
using System.Text.RegularExpressions;

namespace Agent.Api.Features.Knowledge.Chunking;

public sealed partial class ChunkingService
    : IChunkingService
{
    public IReadOnlyList<string> Split(
        string text,
        int chunkSize = 800,
        int overlap = 150)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        if (chunkSize < 100)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkSize));
        }

        if (overlap < 0 || overlap >= chunkSize)
        {
            throw new ArgumentOutOfRangeException(nameof(overlap));
        }

        var normalized = Normalize(text);

        var paragraphs = ParagraphRegex
            .Split(normalized)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        var chunks = new List<string>();

        var builder = new StringBuilder();

        foreach (var paragraph in paragraphs)
        {
            if (builder.Length > 0 &&
                builder.Length + paragraph.Length + 2 > chunkSize)
            {
                AddChunk(
                    chunks,
                    builder.ToString());

                var overlapText =
                    GetOverlap(
                        builder.ToString(),
                        overlap);

                builder.Clear();

                if (!string.IsNullOrWhiteSpace(overlapText))
                {
                    builder.Append(overlapText);
                }
            }

            if (paragraph.Length > chunkSize)
            {
                FlushBuilder(
                    chunks,
                    builder);

                foreach (var chunk in SplitLargeParagraph(
                             paragraph,
                             chunkSize,
                             overlap))
                {
                    AddChunk(
                        chunks,
                        chunk);
                }

                continue;
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine();
            }

            builder.Append(paragraph);
        }

        FlushBuilder(
            chunks,
            builder);

        return chunks
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static string Normalize(string text)
    {
        var normalized =
            text.Replace("\r\n", "\n");

        // Fix words stuck together after PDF extraction:
        // WorkEmployees -> Work Employees
        normalized =
            MissingSpaceRegex.Replace(
                normalized,
                " ");

        // Collapse spaces/tabs
        normalized =
            HorizontalWhitespaceRegex.Replace(
                normalized,
                " ");

        // Collapse excessive blank lines
        normalized =
            ExcessiveNewLinesRegex.Replace(
                normalized,
                "\n\n");

        return normalized.Trim();
    }

    private static IEnumerable<string> SplitLargeParagraph(
        string text,
        int chunkSize,
        int overlap)
    {
        var start = 0;

        while (start < text.Length)
        {
            var end =
                Math.Min(
                    start + chunkSize,
                    text.Length);

            if (end < text.Length)
            {
                var boundary =
                    text.LastIndexOfAny(
                        ['.', '!', '?'],
                        end - 1,
                        end - start);

                if (boundary >
                    start + chunkSize / 2)
                {
                    end = boundary + 1;
                }
            }

            var chunk =
                text[start..end].Trim();

            if (chunk.Length > 0)
            {
                yield return chunk;
            }

            if (end >= text.Length)
            {
                yield break;
            }

            start =
                Math.Max(
                    end - overlap,
                    start + 1);
        }
    }

    private static string GetOverlap(
        string text,
        int overlap)
    {
        if (overlap <= 0 ||
            text.Length <= overlap)
        {
            return text;
        }

        var start =
            text.Length - overlap;

        var boundary =
            text.IndexOf(
                ' ',
                start);

        if (boundary >= 0 &&
            boundary < text.Length - 1)
        {
            start = boundary + 1;
        }

        return text[start..]
            .Trim();
    }

    private static void FlushBuilder(
        ICollection<string> chunks,
        StringBuilder builder)
    {
        if (builder.Length == 0)
        {
            return;
        }

        AddChunk(
            chunks,
            builder.ToString());

        builder.Clear();
    }

    private static void AddChunk(
        ICollection<string> chunks,
        string content)
    {
        var chunk =
            content.Trim();

        if (chunk.Length > 0)
        {
            chunks.Add(chunk);
        }
    }

    private static readonly Regex HorizontalWhitespaceRegex =
        new(@"[ \t]+", RegexOptions.Compiled);

    private static readonly Regex ExcessiveNewLinesRegex =
        new(@"\n{3,}", RegexOptions.Compiled);

    // Blank line = paragraph
    private static readonly Regex ParagraphRegex =
        new(@"\n\s*\n", RegexOptions.Compiled);

    // WorkEmployees -> Work Employees
    private static readonly Regex MissingSpaceRegex =
        new(@"(?<=[a-z])(?=[A-Z])", RegexOptions.Compiled);
}