using ContractClause.Application.Common.Interfaces;
using ContractClause.Application.Options;
using ContractClause.Infrastructure.Ai;
using ContractClause.Infrastructure.Blob;
using ContractClause.Infrastructure.External.Fatianshi;
using ContractClause.Infrastructure.Options;
using ContractClause.Infrastructure.Persistence;
using ContractClause.Infrastructure.Persistence.Repositories;
using ContractClause.Infrastructure.VectorStore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContractClause.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<VectorStoreOptions>(configuration.GetSection(VectorStoreOptions.SectionName));
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        services.Configure<FatianshiTemplateSyncOptions>(configuration.GetSection(FatianshiTemplateSyncOptions.SectionName));
        services.Configure<TemplateBlobOptions>(configuration.GetSection(TemplateBlobOptions.SectionName));

        var dbOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
            ?? new DatabaseOptions();

        services.AddDbContext<AppDbContext>(opt =>
        {
            if (dbOptions.Provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
                opt.UseNpgsql(dbOptions.ConnectionString);
            else
                throw new NotSupportedException($"Database provider '{dbOptions.Provider}' is not supported.");
        });

        services.AddScoped<ITemplateRepository, TemplateRepository>();
        services.AddScoped<IClauseRepository, ClauseRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<IImportTaskRepository, ImportTaskRepository>();
        services.AddScoped<ITemplateSyncStateRepository, TemplateSyncStateRepository>();

        services.AddHttpClient<IFatianshiTemplateApiClient, FatianshiTemplateApiClient>((sp, client) =>
        {
            var syncOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<FatianshiTemplateSyncOptions>>().Value;
            client.BaseAddress = new Uri(syncOptions.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromMinutes(5);
            if (!string.IsNullOrWhiteSpace(syncOptions.ApiKey))
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", syncOptions.ApiKey);
            }
        });
        services.AddSingleton<IVectorStore, QdrantVectorStore>();
        services.AddSingleton<IEmbeddingService, OpenAiEmbeddingService>();
        services.AddSingleton<ITextGenerationService, OpenAiTextGenerationService>();
        services.AddSingleton<AzureTemplateBlobStorage>();
        services.AddSingleton<ITemplateSourceBlobReader>(sp => sp.GetRequiredService<AzureTemplateBlobStorage>());
        services.AddSingleton<ITemplateProcessedBlobWriter>(sp => sp.GetRequiredService<AzureTemplateBlobStorage>());

        var aiOptions = configuration.GetSection(AiOptions.SectionName).Get<AiOptions>() ?? new AiOptions();
        services.AddHttpClient("openai", client =>
        {
            client.BaseAddress = new Uri(aiOptions.EmbeddingModel.BaseUrl.TrimEnd('/') + "/");
            if (!string.IsNullOrWhiteSpace(aiOptions.EmbeddingModel.ApiKey))
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", aiOptions.EmbeddingModel.ApiKey);
        });

        services.AddHttpClient("openai-text", client =>
        {
            client.BaseAddress = new Uri(aiOptions.TextModel.BaseUrl.TrimEnd('/') + "/");
            if (!string.IsNullOrWhiteSpace(aiOptions.TextModel.ApiKey))
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", aiOptions.TextModel.ApiKey);
        });

        return services;
    }
}
