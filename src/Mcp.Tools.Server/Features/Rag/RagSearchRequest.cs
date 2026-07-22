using System.ComponentModel.DataAnnotations;

namespace Mcp.Tools.Server.Features.Rag;

public sealed class RagSearchRequest
{
    [Required, MinLength(2)]
    public string Query { get; init; } = string.Empty;

    [Range(1,20)]
    public int TopK { get; init; } = 5;
}
