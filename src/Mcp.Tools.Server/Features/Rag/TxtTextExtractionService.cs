using System.Text;

namespace Mcp.Tools.Server.Features.Rag;

public sealed class TxtTextExtractionService : ITextExtractionService
{
    private const long MaxFileSizeBytes = 2 * 1024 * 1024;

    public async Task<string> ExtractAsync(IFormFile file,CancellationToken cancellationToken=default)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (file.Length == 0) throw new ArgumentException("The uploaded file is empty.", nameof(file));
        if (file.Length > MaxFileSizeBytes) throw new ArgumentException($"The uploaded file exceeds {MaxFileSizeBytes} bytes.", nameof(file));
        if (!string.Equals(Path.GetExtension(file.FileName), ".txt", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Step 8.1 supports TXT files only.", nameof(file));

        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        var text = await reader.ReadToEndAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("The TXT file does not contain readable text.", nameof(file));
        return text;
    }
}
