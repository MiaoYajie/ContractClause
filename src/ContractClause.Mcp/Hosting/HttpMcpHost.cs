using ContractClause.Application.Common.Interfaces;
using ContractClause.Infrastructure.Persistence;
using ContractClause.Mcp.Auth;
using ContractClause.Mcp.Options;
using ContractClause.Mcp.Services;
using ContractClause.Mcp.Tools;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

namespace ContractClause.Mcp.Hosting;

public static class HttpMcpHost
{
    public static async Task RunAsync(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var mcpOptions = builder.Configuration.GetSection(McpOptions.SectionName).Get<McpOptions>() ?? new McpOptions();
        var httpPath = string.IsNullOrWhiteSpace(mcpOptions.HttpPath) ? "/mcp" : mcpOptions.HttpPath.TrimEnd('/');

        builder.WebHost.UseUrls($"http://0.0.0.0:{mcpOptions.HttpPort}");

        builder.Services.AddContractClauseMcpCore(builder.Configuration);
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IUserContext, HttpMcpUserContext>();

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>();

        builder.Services
            .AddMcpServer()
            .WithHttpTransport()
            .WithTools<ContractClauseTools>();

        var app = builder.Build();

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        app.MapHealthChecks("/healthz");
        app.UseMiddleware<McpHttpAuthMiddleware>();
        app.MapMcp(httpPath);

        await app.RunAsync();
    }
}
