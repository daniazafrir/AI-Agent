using Microsoft.AspNetCore.Mvc;

namespace Mcp.Tools.Server.Features.Rag;

[ApiController]
[Route("api/rag")]
public sealed class RagController(
    IRagService ragService) : ControllerBase
{
 
 

    [HttpDelete("documents/{documentId:guid}")]
    public async Task<IActionResult> DeleteDocument(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var deleted = await ragService.DeleteVectorsAsync(
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