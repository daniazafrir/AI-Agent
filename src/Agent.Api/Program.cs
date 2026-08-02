using Agent.Api.Chat;
using Agent.Api.Configuration;
using Agent.Api.Conversations;
using Agent.Api.Features.Conversation;
using Agent.Api.Infrastructure.Persistence;
using Agent.Api.Mcp;
using Agent.Api.OpenAI;
using Agent.Api.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using OpenAI.Chat;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.Configure<OpenAiOptions>(
    builder.Configuration.GetSection(
        OpenAiOptions.SectionName));

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

builder.Services.AddSingleton<ChatClient>(
    serviceProvider =>
    {
        var options = serviceProvider
            .GetRequiredService<
                Microsoft.Extensions.Options
                    .IOptions<OpenAiOptions>>()
            .Value;

        var apiKey =
            !string.IsNullOrWhiteSpace(options.ApiKey)
                ? options.ApiKey
                : Environment.GetEnvironmentVariable(
                    "OPENAI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "OpenAI API key is missing. " +
                "Configure OpenAI:ApiKey or OPENAI_API_KEY.");
        }

        return new ChatClient(
            options.Model,
            apiKey);
    });

builder.Services.AddScoped<
    IConversationStore,
    PostgresConversationStore>();

builder.Services.AddSingleton<
    IMcpToolClient,
    McpToolClient>();

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();