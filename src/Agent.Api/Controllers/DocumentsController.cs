using Agent.Api.Mcp;
using Microsoft.AspNetCore.Mvc;

namespace Agent.Api.Controllers;

[ApiController]
[Route("api/documents")]
public sealed class DocumentsController : ControllerBase
{
    private const long MaxFileSize = 2 * 1024 * 1024;

    private readonly IMcpToolClient _mcpToolClient;

    public DocumentsController(IMcpToolClient mcpToolClient)
    {
        _mcpToolClient = mcpToolClient;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new
            {
                message = "?? ???? ???? ?? ?????? ???."
            });
        }

        if (file.Length > MaxFileSize)
        {
            return BadRequest(new
            {
                message = "???? ????? ????? ??? 2MB."
            });
        }

        var extension = Path.GetExtension(file.FileName);

        if (!string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = "???? ?? ???? ?????? ????? TXT ????."
            });
        }

        string content;

        await using (var stream = file.OpenReadStream())
        using (var reader = new StreamReader(stream))
        {
            content = await reader.ReadToEndAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return BadRequest(new
            {
                message = "????? ???? ???? ????."
            });
        }

        var arguments = new Dictionary<string, object?>
        {
            ["fileName"] = file.FileName,
            ["content"] = content
        };

        var result = await _mcpToolClient.CallToolAsync(
            "index_document",
            arguments,
            cancellationToken);

        return Ok(result);
    }
}