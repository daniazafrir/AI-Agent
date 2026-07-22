namespace Agent.Api.Mcp;
public sealed class McpOptions
{
 public const string SectionName = "Mcp";
 public string ServerUrl { get; init; } = "http://localhost:5100/mcp";
 public TimeSpan ConnectionTimeout { get; init; } = TimeSpan.FromSeconds(30);
}
