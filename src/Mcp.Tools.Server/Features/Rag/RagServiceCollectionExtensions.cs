using Agent.Knowledge.Infrastructure.Persistence;
using Agent.Knowledge.Repositories;
using Agent.Knowledge.Search.Keyword;
using Mcp.Tools.Server.Features.Rag;
using Mcp.Tools.Server.Features.Rag.Hybrid;
using Mcp.Tools.Server.Features.Rag.Keyword;
using Mcp.Tools.Server.Features.Rag.Ranking;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;
using Qdrant.Client;


namespace Mcp.Tools.Server.Features.Rag;

public static class RagServiceCollectionExtensions
{
    public static IServiceCollection AddRagServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        //
        // Configuration
        //

        services.Configure<QdrantOptions>(
            configuration.GetSection(
                QdrantOptions.SectionName));

        //
        // OpenAI Embeddings
        //

        services.AddSingleton<EmbeddingClient>(
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

        services.Configure<RagOptions>(
            configuration.GetSection("Rag"));


        services.AddSingleton<
            IEmbeddingService,
            OpenAiEmbeddingService>();

        //
        // Text extraction and chunking
        //

        services.AddSingleton<
            ITextExtractionService,
            TxtTextExtractionService>();


        //
        // Qdrant
        //

        services.AddSingleton<QdrantClient>(serviceProvider =>
        {
            var options =
                serviceProvider
                    .GetRequiredService<IOptions<QdrantOptions>>()
                    .Value;

            return new QdrantClient(
                options.Host,
                options.GrpcPort);
        });

        services.AddSingleton<
            IVectorStore,
            QdrantVectorStore>();

        services.AddScoped<IHybridSearchService, HybridSearchService>();

        services.AddSingleton<RrfRanker>();
        //
        // RAG
        //


        var connectionString =
            configuration.GetConnectionString(
                "AgentDatabase")
            ?? throw new InvalidOperationException(
                "Connection string 'AgentDatabase' is missing.");

        services.AddDbContext<AgentDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<
            IRagService,
            RagService>();

        services.AddScoped<
            IKnowledgeDocumentRepository,
            PostgresKnowledgeDocumentRepository>();


        services.AddScoped<
            IKnowledgeChunkRepository,
            PostgresKnowledgeChunkRepository>();

        services.AddScoped<
            IKeywordSearchService,
            KeywordSearchService>();
        return services;
    }
}

