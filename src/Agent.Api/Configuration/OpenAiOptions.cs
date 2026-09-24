using System.ComponentModel.DataAnnotations;

namespace Agent.Api.Configuration;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAI";
    public bool EnablePromptViewer { get; set; }

    [Required]
    public string Model { get; init; } = string.Empty;

    public string? ApiKey { get; init; } = string.Empty;

    [Range(1, 10)]
    public int MaxToolRounds { get; init; } = 5;
}
