using ContractClause.Application.Common.Interfaces;
using ContractClause.Infrastructure.Persistence;
using ContractClause.Mcp.Auth;
using ContractClause.Mcp.Services;
using ContractClause.Mcp.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;

namespace ContractClause.Mcp.Hosting;

public static class StdioMcpHost
{
    public static async Task RunAsync(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddContractClauseMcpCore(builder.Configuration);
        builder.Services.AddSingleton<McpAuthState>();
        builder.Services.AddSingleton<McpUserContext>();
        builder.Services.AddSingleton<IUserContext>(sp => sp.GetRequiredService<McpUserContext>());
        builder.Services.AddHostedService<McpAuthBootstrap>();

        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithTools<ContractClauseTools>();

        var host = builder.Build();
        await EnsureDatabaseAsync(host.Services);
        await host.RunAsync();
    }

    private static async Task EnsureDatabaseAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
    }
}
