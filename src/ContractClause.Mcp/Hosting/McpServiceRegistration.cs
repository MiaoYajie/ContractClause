using ContractClause.Application;
using ContractClause.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContractClause.Mcp.Hosting;

public static class McpServiceRegistration
{
    public static IServiceCollection AddContractClauseMcpCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddApplication();
        services.AddInfrastructure(configuration);
        return services;
    }
}
