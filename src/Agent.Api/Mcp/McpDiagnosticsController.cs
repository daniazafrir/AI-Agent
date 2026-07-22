using Microsoft.AspNetCore.Mvc;

namespace Agent.Api.Mcp;

[ApiController]
[Route("api/mcp")]
public sealed class McpDiagnosticsController(
    IMcpToolClient mcpToolClient)
    : ControllerBase
{
    [HttpGet("tools")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetTools(
        CancellationToken cancellationToken)
    {
        var tools =
            await mcpToolClient.ListToolsAsync(
                cancellationToken);

        return Ok(tools);
    }

    [HttpPost("time")]
    public async Task<ActionResult<string>> GetCurrentTime(
        CancellationToken cancellationToken)
    {
        var result =
            await mcpToolClient.CallToolAsync(
                "get_current_time",
                new Dictionary<string, object?>
                {
                    ["timeZone"] = "Asia/Jerusalem"
                },
                cancellationToken);

        return Ok(result);
    }
}