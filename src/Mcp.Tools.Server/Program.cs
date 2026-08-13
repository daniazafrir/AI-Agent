using Mcp.Tools.Server.Features.Rag;
using Mcp.Tools.Server.Tools;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;
using Qdrant.Client;

var builder = WebApplication.CreateBuilder(args);

//
// Configuration
//

builder.Services.Configure<QdrantOptions>(
    builder.Configuration.GetSection(
        QdrantOptions.SectionName));

//
// OpenAI Embeddings
//

builder.Services.AddSingleton<EmbeddingClient>(
    serviceProvider =>
    {
        var configuration =
            serviceProvider
                .GetRequiredService<IConfiguration>();

        var configuredApiKey =
            configuration["OpenAI:ApiKey"];

        var apiKey =
            !string.IsNullOrWhiteSpace(configuredApiKey)
                ? configuredApiKey
                : Environment.GetEnvironmentVariable(
                    "OPENAI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "OpenAI API key is missing. " +
                "Configure OpenAI:ApiKey or OPENAI_API_KEY.");
        }

        var embeddingModel =
            configuration["OpenAI:EmbeddingModel"];

        if (string.IsNullOrWhiteSpace(embeddingModel))
        {
            embeddingModel =
                "text-embedding-3-small";
        }

        return new EmbeddingClient(
            embeddingModel,
            apiKey);
    });
builder.Services.AddSingleton<
    IEmbeddingService,
    OpenAiEmbeddingService>();

//
// Text extraction and chunking
//

builder.Services.AddSingleton<
    ITextExtractionService,
    TxtTextExtractionService>();

builder.Services.AddSingleton<
    IChunkingService,
    ChunkingService>();

//
// Qdrant
//

builder.Services.AddSingleton<QdrantClient>(serviceProvider =>
{
    var options =
        serviceProvider
            .GetRequiredService<IOptions<QdrantOptions>>()
            .Value;

    return new QdrantClient(
        options.Host,
        options.GrpcPort);
});

builder.Services.AddSingleton<
    IVectorStore,
    QdrantVectorStore>();

//
// RAG
//

builder.Services.AddSingleton<
    IRagDocumentStore,
    InMemoryRagDocumentStore>();

builder.Services.AddSingleton<
    IRagService,
    RagService>();

//
// MCP
//

builder.Services
    .AddMcpServer()
    .WithHttpTransport(options =>
    {
        options.Stateless = true;
    })
    .WithTools<TimeTools>()
    .WithTools<CalculatorTools>()
    .WithTools<KnowledgeTools>()
    .WithTools<DocumentTools>();

var app = builder.Build();

app.MapGet(
    "/health",
    () => Results.Ok(new
    {
        status = "healthy"
    }));

app.MapMcp("/mcp");

app.Run();