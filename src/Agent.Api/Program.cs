using Agent.Api.Chat;
using Agent.Api.Configuration;
using Agent.Api.Configuration.Validators;
using Agent.Api.Conversations;
using Agent.Api.Features.Conversation;
using Agent.Api.Features.Knowledge;
using Agent.Api.Features.Knowledge.Chunking;
using Agent.Api.Features.Knowledge.Extraction;
using Agent.Api.HealthChecks;
using Agent.Api.Infrastructure.Exceptions;
using Agent.Api.Infrastructure.Middleware;
using Agent.Api.Infrastructure.Persistence;
using Agent.Api.Knowledge;
using Agent.Api.Mcp;
using Agent.Api.OpenAI;
using Agent.Api.Tools;
using Agent.Knowledge.Repositories;
using Agent.Knowledge.Search.Keyword;
using Mcp.Tools.Server.Features.Rag.Keyword;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using OpenAI.Chat;
using Serilog;
using KnowledgeDbContext =
    Agent.Knowledge.Infrastructure.Persistence.AgentDbContext;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog(
    (context, configuration) =>
    {
        configuration.ReadFrom.Configuration(
            context.Configuration);
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Agent API",
        Version = "v1"
    });
});

builder.Services.Configure<AgentOptions>(
    builder.Configuration.GetSection(
        AgentOptions.SectionName));

builder.Services
    .AddOptions<OpenAiOptions>()
    .Bind(
        builder.Configuration.GetSection(
            OpenAiOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options =>
            !string.IsNullOrWhiteSpace(options.Model),
        "OpenAI model is required.")
    .ValidateOnStart();

builder.Services.PostConfigure<OpenAiOptions>(options =>
    options.EnablePromptViewer =
        builder.Configuration.GetValue<bool?>("OpenAI:EnablePromptViewer") ??
        builder.Environment.IsDevelopment());

builder.Services.Configure<McpOptions>(
    builder.Configuration.GetSection(
        McpOptions.SectionName));

builder.Services.Configure<CorsOptions>(
    builder.Configuration.GetSection(
        CorsOptions.SectionName));

var allowedOrigins =
    builder.Configuration
        .GetSection("AllowedOrigins")
        .Get<string[]>()
    ?? [];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins.Length == 0)
        {
            policy.AllowAnyOrigin();
        }
        else
        {
            policy.WithOrigins(allowedOrigins);
        }

        policy
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var connectionString =
    builder.Configuration.GetConnectionString(
        "AgentDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'AgentDatabase' is missing.");

builder.Services.AddDbContext<AgentDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddDbContext<KnowledgeDbContext>(
    options =>
        options.UseNpgsql(connectionString));

builder.Services.AddSingleton<ChatClient>(
    serviceProvider =>
    {
        var options = serviceProvider
            .GetRequiredService<IOptions<OpenAiOptions>>()
            .Value;

        return new ChatClient(
            options.Model,
            options.ApiKey);
    });

builder.Services.AddSingleton<
    IValidateOptions<OpenAiOptions>,
    OpenAiOptionsValidator>();


builder.Services.AddScoped<
    IConversationStore,
    PostgresConversationStore>();
builder.Services
    .AddHttpClient("Mcp")
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.DisableForUnsafeHttpMethods();
    });

builder.Services.AddSingleton<
    IMcpToolClient,
    McpToolClient>();

builder.Services.AddSingleton<
    IMcpToolRegistry,
    McpToolRegistry>();

builder.Services.AddSingleton<
    IMcpToolRegistry,
    McpToolRegistry>();

builder.Services.AddScoped<
    IToolExecutor,
    ToolExecutor>();

builder.Services.AddScoped<
    IConversationService,
    ConversationService>();

builder.Services.AddScoped<
    IAgentRuntime,
    AgentRuntime>();

builder.Services.AddScoped<
    IChatOrchestrator,
    ChatOrchestrator>();

builder.Services.AddScoped
    <IChatCompletionService,
    OpenAiChatCompletionService>();

builder.Services.AddScoped<
    IToolProcessor,
    ToolProcessor>();

builder.Services.AddScoped<
    IAgentLoop,
    AgentLoop>();

builder.Services.AddSingleton<
    IToolRouter,
    ToolRouter>();

builder.Services.AddScoped<
    IKnowledgeDocumentRepository,
    PostgresKnowledgeDocumentRepository>();

builder.Services.AddScoped<
    IKnowledgeDocumentService,
    KnowledgeDocumentService>();

builder.Services.AddScoped<
    IKnowledgeChunkRepository,
    PostgresKnowledgeChunkRepository>();

builder.Services.AddScoped<
    IKeywordSearchService,
    KeywordSearchService>();

builder.Services.AddSingleton<IChunkingService, ChunkingService>();

builder.Services.AddSingleton<
    IDocumentTextExtractor,
    TxtDocumentTextExtractor>();

builder.Services.AddSingleton<
    IDocumentTextExtractor,
    PdfDocumentTextExtractor>();

builder.Services.AddSingleton<
    DocumentTextExtractorFactory>();

builder.Services
    .AddHealthChecks()

    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(
            "Agent API is running."),
        tags: ["live"])

    .AddCheck<PostgresHealthCheck>(
        "postgres",
        tags: ["ready"])

    .AddCheck<McpHealthCheck>(
        "mcp",
        tags: ["ready"])

    .AddCheck<OpenAiConfigurationHealthCheck>(
        "openai-configuration",
        tags: ["ready"]);

builder.Services.AddExceptionHandler<
    GlobalExceptionHandler>();

builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseCorrelationId();

app.UseExceptionHandler();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.MapControllers();

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = registration =>
            registration.Tags.Contains("live")
    });

app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = registration =>
            registration.Tags.Contains("ready"),

        ResponseWriter =
            HealthCheckResponseWriter.WriteAsync
    });

app.MapHealthChecks(
    "/health",
    new HealthCheckOptions
    {
        Predicate = _ => true,

        ResponseWriter =
            HealthCheckResponseWriter.WriteAsync
    });

app.Run();
