namespace Mcp.Tools.Server.Features.Rag;

public sealed class RagOptions
{
    public const string SectionName = "Rag";

    public double MinimumVectorScore { get; set; } = 0.45;
}