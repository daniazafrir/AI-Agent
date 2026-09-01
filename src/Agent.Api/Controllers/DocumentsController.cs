using Agent.Api.Features.Knowledge;
using Agent.Api.Infrastructure.Hashing;
using Agent.Api.Mcp;
using Microsoft.AspNetCore.Mvc;

namespace Agent.Api.Controllers;

[ApiController]
[Route("api/documents")]
public sealed class DocumentsController(
    IKnowledgeDocumentService knowledgeDocumentService)
    : ControllerBase
{
    private const long MaxFileSize =
        2 * 1024 * 1024;

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
                message = "File is required."
            });
        }

        if (file.Length > MaxFileSize)
        {
            return BadRequest(new
            {
                message = "File size cannot exceed 2MB."
            });
        }

        var extension =
            Path.GetExtension(
                file.FileName);

        if (!SupportedExtensions.Contains(
                extension,
                StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = "Supported file types: TXT, PDF."
            });
        }

        await using var stream =
            file.OpenReadStream();

        var result =
            await knowledgeDocumentService.UploadAsync(
                file.FileName,
                stream,
                cancellationToken);

        return Ok(result);
    }

    [HttpGet]
        public async Task<IActionResult> GetDocuments(
            CancellationToken cancellationToken)
        {
            var documents =
                await knowledgeDocumentService.ListAsync(
                    cancellationToken);

            return Ok(documents);
        }
    

    [HttpDelete("{documentId:guid}")]
    public async Task<IActionResult> Delete(
    Guid documentId,
    CancellationToken cancellationToken)
    {
        var result =
            await knowledgeDocumentService.DeleteAsync(
               documentId,               
                cancellationToken);

        return Ok(result);
    }

    [HttpGet("{documentId:guid}/chunks/{chunkIndex:int}")]
    public async Task<IActionResult> GetChunk(
    Guid documentId,
    int chunkIndex,
    CancellationToken cancellationToken)
    {
        var chunk =
            await knowledgeDocumentService
                .GetChunkAsync(
                    documentId,
                    chunkIndex,
                    cancellationToken);

        if (chunk is null)
        {
            return NotFound();
        }

        return Ok(chunk);
    }

    [HttpPost("{documentId:guid}/reindex")]
    public async Task<IActionResult> Reindex(
    Guid documentId,
    CancellationToken cancellationToken)
    {
        var success =
            await knowledgeDocumentService.ReindexAsync(
                documentId,
                cancellationToken);

        if (!success)
        {
            return NotFound(new
            {
                message = "Document not found."
            });
        }

        return Ok(new
        {
            success = true,
            documentId,
            message = "Document re-indexed successfully."
        });
    }

    private static readonly string[] SupportedExtensions =
    [
        ".txt",
        ".pdf"
    ];
}