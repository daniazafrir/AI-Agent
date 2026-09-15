using Mcp.Tools.Server.Features.Rag;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRagServices(builder.Configuration);
//
// MCP
//

builder.Services
    .AddMcpServer()
    .WithHttpTransport(options =>
    {
        options.Stateless = true;
    })
    .WithToolsFromAssembly();

var app = builder.Build();

app.MapGet(
    "/health",
    () => Results.Ok(new
    {
        status = "healthy"
    }));

app.MapMcp("/mcp");

app.Run();
