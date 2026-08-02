namespace Agent.Api.OpenAI;

public sealed class ToolCallResult
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public BinaryData Arguments { get; init; } =
        BinaryData.FromString("{}");
}