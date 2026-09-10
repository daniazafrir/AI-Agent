using Agent.Knowledge.Infrastructure.Persistence;
using Agent.Knowledge.Repositories;
using Agent.Knowledge.Search.Keyword;
using Mcp.Tools.Server.Features.Rag;
using Mcp.Tools.Server.Features.Rag.Hybrid;
using Mcp.Tools.Server.Features.Rag.Ranking;
using Mcp.Tools.Server.Tools;
using Microsoft.EntityFrameworkCore;
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

builder.Services.AddSingleton<IHybridSearchService, HybridSearchService>();

builder.Services.AddSingleton<RrfRanker>();
//
// RAG
//


var connectionString =
    builder.Configuration.GetConnectionString(
        "AgentDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'AgentDatabase' is missing.");

builder.Services.AddDbContext<AgentDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddSingleton<
    IRagService,
    RagService>();

builder.Services.AddScoped<
    IKnowledgeDocumentRepository,
    PostgresKnowledgeDocumentRepository>();


builder.Services.AddScoped<
    IKnowledgeChunkRepository,
    PostgresKnowledgeChunkRepository>();

builder.Services.AddScoped<
    IKeywordSearchService,
    KeywordSearchService>();


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