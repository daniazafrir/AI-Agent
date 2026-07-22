using Microsoft.AspNetCore.Mvc;

namespace Mcp.Tools.Server.Features.Rag;

[ApiController]
[Route("api/rag")]
public sealed class RagController(
    IRagService ragService) : ControllerBase
{
    [HttpPost("documents")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<ActionResult<DocumentInfo>> UploadDocument(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return BadRequest(new
            {
                error = "A file is required."
            });
        }

        try
        {
            var document = await ragService.UploadAsync(
                file,
                cancellationToken);

            return CreatedAtAction(
                nameof(ListDocuments),
                new { },
                document);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                error = exception.Message
            });
        }
    }

    [HttpGet("documents")]
    public async Task<ActionResult<IReadOnlyList<DocumentInfo>>> ListDocuments(
        CancellationToken cancellationToken)
    {
        var documents = await ragService.ListDocumentsAsync(
            cancellationToken);

        return Ok(documents);
    }

    [HttpDelete("documents/{documentId:guid}")]
    public async Task<IActionResult> DeleteDocument(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var deleted = await ragService.DeleteDocumentAsync(
            documentId,
            cancellationToken);

        return deleted
            ? NoContent()
            : NotFound();
    }

    [HttpPost("search")]
    public async Task<ActionResult<RagSearchResponse>> Search(
        RagSearchRequest request,
        CancellationToken cancellationToken)
    {
        var matches = await ragService.SearchAsync(
            request.Query,
            request.TopK,
            cancellationToken);

        return Ok(
            new RagSearchResponse(
                request.Query,
                matches));
    }
}