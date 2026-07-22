namespace Mcp.Tools.Server.Features.Rag;

public interface ITextExtractionService
{
    Task<string> ExtractAsync(IFormFile file,CancellationToken cancellationToken=default);
}
