using ContractClause.Application.Common.Behaviors;
using ContractClause.Application.Common.Interfaces;
using ContractClause.Application.Templates;
using ContractClause.Application.Templates.Processing;
using ContractClause.Application.Templates.Sync;
using Microsoft.Extensions.DependencyInjection;

namespace ContractClause.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(UserContextPipelineBehavior<,>));
        });

        services.AddScoped<ITemplateContentImporter, TemplateContentImporter>();
        services.AddScoped<ITemplateSyncService, TemplateSyncService>();
        services.AddScoped<ITemplateContentProcessingService, TemplateContentProcessingService>();

        return services;
    }
}
