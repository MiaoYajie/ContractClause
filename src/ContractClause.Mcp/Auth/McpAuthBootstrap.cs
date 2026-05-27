using ContractClause.Application.Common.Interfaces;
using ContractClause.Mcp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ContractClause.Mcp.Auth;

public class McpAuthBootstrap(IServiceProvider services) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var apiKeyValue = Environment.GetEnvironmentVariable("MCP_API_KEY")
            ?? throw new InvalidOperationException("MCP_API_KEY 环境变量未设置");

        await using var scope = services.CreateAsyncScope();
        var apiKeys = scope.ServiceProvider.GetRequiredService<IApiKeyRepository>();
        var authState = scope.ServiceProvider.GetRequiredService<McpAuthState>();

        var apiKey = await apiKeys.GetByKeyAsync(apiKeyValue, cancellationToken);
        if (apiKey is null)
            throw new UnauthorizedAccessException("API Key 无效");

        authState.OwnerId = apiKey.OwnerId;
        authState.OwnerType = apiKey.OwnerType;
        authState.IsAuthenticated = true;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
