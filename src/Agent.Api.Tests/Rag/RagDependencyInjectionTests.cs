using Mcp.Tools.Server.Features.Rag;
using Mcp.Tools.Server.Features.Rag.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Agent.Api.Tests.Rag;

public sealed class RagDependencyInjectionTests
{
    [Fact]
    public void Registration_ResolvesRagWithinScope_AndDoesNotShareScopedServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenAI:ApiKey"] = "test-key-no-network-calls",
                ["ConnectionStrings:AgentDatabase"] =
                    "Host=localhost;Database=di_validation;Username=test;Password=test"
            }).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddRagServices(configuration);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        var rag = first.ServiceProvider.GetRequiredService<IRagService>();
        Assert.Same(rag, first.ServiceProvider.GetRequiredService<IRagService>());
        Assert.NotSame(rag, second.ServiceProvider.GetRequiredService<IRagService>());
        Assert.NotSame(
            first.ServiceProvider.GetRequiredService<IHybridSearchService>(),
            second.ServiceProvider.GetRequiredService<IHybridSearchService>());
    }
}
