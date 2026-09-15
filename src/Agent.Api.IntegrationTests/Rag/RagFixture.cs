using Mcp.Tools.Server.Features.Rag;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Agent.Api.IntegrationTests.Rag;

public sealed class RagFixture : IDisposable
{
    private readonly ServiceProvider _provider;

    public RagFixture()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("ragsettings.json", optional: false)
            .AddUserSecrets(typeof(RagService).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();

        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrWhiteSpace(apiKey))
            configuration["OpenAI:ApiKey"] = apiKey;
        if (string.IsNullOrWhiteSpace(configuration["OpenAI:ApiKey"]))
            throw new InvalidOperationException(
                "Configure OpenAI:ApiKey in Mcp.Tools.Server user secrets, " +
                "or set OPENAI_API_KEY / OpenAI__ApiKey before running tests.");
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddRagServices(configuration);
        _provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
    }

    public IServiceScope CreateScope() => _provider.CreateScope();
    public void Dispose() => _provider.Dispose();
}

