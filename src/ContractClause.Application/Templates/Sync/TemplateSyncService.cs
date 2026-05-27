using ContractClause.Application.Common.Interfaces;
using ContractClause.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContractClause.Application.Templates.Sync;

public class TemplateSyncService(
    IFatianshiTemplateApiClient api,
    ITemplateContentImporter importer,
    ITemplateRepository templates,
    ITemplateSyncStateRepository syncState,
    IOptions<FatianshiTemplateSyncOptions> options,
    ILogger<TemplateSyncService> logger) : ITemplateSyncService
{
    private readonly FatianshiTemplateSyncOptions _options = options.Value;

    public async Task<TemplateSyncRunResult> RunAsync(CancellationToken ct = default)
    {
        var runStarted = DateTime.UtcNow;
        var state = await syncState.GetOrCreateAsync(ct);
        var watermark = state.LastSyncedAt;
        var processed = 0;
        var skipped = 0;
        var failed = 0;
        var errors = new List<string>();

        var page = 1;
        while (true)
        {
            var batch = await api.SearchUpdatedAsync(watermark, page, _options.PageSize, ct);
            if (batch.Count == 0) break;

            foreach (var item in batch)
            {
                try
                {
                    var existing = await templates.GetByExternalIdAsync(item.Id, ct);
                    var sourceUpdated = item.UpdatedAt;
                    if (existing is not null && sourceUpdated.HasValue && existing.SourceUpdatedAt.HasValue &&
                        existing.SourceUpdatedAt >= sourceUpdated)
                    {
                        skipped++;
                        continue;
                    }

                    var detail = await api.GetTemplateAsync(item.Id, ct);
                    sourceUpdated ??= detail.UpdatedAt;

                    var type = detail.Type ?? item.Type ?? _options.DefaultType;
                    var categories = detail.Categories.Count > 0 ? detail.Categories : item.Categories;
                    var tags = detail.Tags.Count > 0 ? detail.Tags : item.Tags;

                    await importer.ImportOrUpdateAsync(new TemplateImportRequest(
                        detail.Html,
                        detail.Title,
                        type,
                        categories,
                        tags,
                        _options.MarkAsOfficial,
                        OwnerId: null,
                        ExternalId: detail.Id,
                        SourceUpdatedAt: sourceUpdated,
                        ExistingTemplateId: existing?.Id), ct);

                    processed++;
                    logger.LogInformation("已同步法天使模板 {ExternalId} ({Title})", detail.Id, detail.Title);
                }
                catch (Exception ex)
                {
                    failed++;
                    var msg = $"模板 {item.Id}: {ex.Message}";
                    errors.Add(msg);
                    logger.LogError(ex, "同步法天使模板失败: {ExternalId}", item.Id);
                }
            }

            if (batch.Count < _options.PageSize) break;
            page++;
        }

        state.LastSyncedAt = runStarted;
        state.LastRunAt = DateTime.UtcNow;
        state.LastRunStatus = failed > 0 ? "partial" : "completed";
        state.LastRunProcessed = processed;
        state.LastRunErrors = errors.Take(20).ToList();
        await syncState.SaveAsync(state, ct);

        logger.LogInformation(
            "法天使模板同步完成: processed={Processed}, skipped={Skipped}, failed={Failed}",
            processed, skipped, failed);

        return new TemplateSyncRunResult(processed, skipped, failed, errors);
    }
}
