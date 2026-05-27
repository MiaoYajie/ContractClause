using ContractClause.Mcp.Hosting;
using ContractClause.Mcp.Options;
using Microsoft.Extensions.Configuration;

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
    ?? "Production";

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

var transport = configuration[$"{McpOptions.SectionName}:Transport"]
    ?? Environment.GetEnvironmentVariable("Mcp__Transport")
    ?? "stdio";

var mcpOptions = configuration.GetSection(McpOptions.SectionName).Get<McpOptions>() ?? new McpOptions();
mcpOptions.Transport = transport;

if (mcpOptions.IsHttpTransport)
    await HttpMcpHost.RunAsync(args);
else
    await StdioMcpHost.RunAsync(args);
