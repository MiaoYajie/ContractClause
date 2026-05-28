using ContractClause.Application.Common.Interfaces;
using ContractClause.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace ContractClause.Application.Templates.Sync;

public class TemplateSyncService(
    IFatianshiTemplateApiClient api,
    ITemplateRepository templates,
    IVectorStore vectorStore,
    IEmbeddingService embedding,
    ITemplateSyncStateRepository syncState,
    ITemplateContentProcessingService contentProcessing,
    IOptions<FatianshiTemplateSyncOptions> options,
    ILogger<TemplateSyncService> logger) : ITemplateSyncService
{
    private readonly FatianshiTemplateSyncOptions _options = options.Value;

    public async Task<TemplateSyncRunResult> RunAsync(CancellationToken ct = default)
    {
        //var maxSourceUpdatedAt = await templates.GetMaxSourceUpdatedAtAsync(ct);
        var maxSourceUpdatedAt = await syncState.GetLastRunAtAsync(ct);
        var processed = 0;
        var skipped = 0;
        var failed = 0;
        var contentProcessed = 0;
        var contentFailed = 0;
        var errors = new List<string>();
        var page = 0;
        var stop = false;

        while (!stop)
        {
            var batch = await api.FetchUpdatedTemplatesPageAsync(page, ct);
            if (batch.Items.Count == 0)
                break;

            foreach (var item in batch.Items)
            {
                if (maxSourceUpdatedAt.HasValue && item.PublishedOn <= maxSourceUpdatedAt.Value)
                {
                    stop = true;
                    break;
                }

                try
                {
                    var existing = await templates.GetByIdAsync(item.Id, ct);
                    var now = DateTime.UtcNow;
                    var metadataChanged = false;

                    if (existing is null)
                    {
                        var number = item.Number ?? await templates.GetNextNumberAsync(ct);
                        var entity = TemplateMetadataMapper.ToNewEntity(item, number, now);
                        await templates.AddAsync(entity, ct);
                        await UpsertTemplateVectorAsync(entity, ct);
                        processed++;
                        metadataChanged = true;
                        logger.LogInformation("已新增模板元数据 {TemplateId} ({Title})", item.Id, item.Title);
                    }
                    else if (existing.SourceUpdatedAt.HasValue && existing.SourceUpdatedAt >= item.PublishedOn)
                    {
                        skipped++;
                    }
                    else
                    {
                        TemplateMetadataMapper.ApplyUpdate(existing, item, now);
                        await templates.UpdateAsync(existing, ct);
                        await UpsertTemplateVectorAsync(existing, ct);
                        processed++;
                        metadataChanged = true;
                        logger.LogInformation("已更新模板元数据 {TemplateId} ({Title})", item.Id, item.Title);
                    }

                    if (metadataChanged)
                    {
                        await templates.SaveChangesAsync(ct);
                        var contentResult = await contentProcessing.ProcessAsync(item.Id, item.Version, ct);
                        if (contentResult.Success)
                            contentProcessed++;
                        else
                        {
                            contentFailed++;
                            if (!string.IsNullOrWhiteSpace(contentResult.Error))
                                errors.Add($"模板 {item.Id} 内容处理: {contentResult.Error}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add($"模板 {item.Id}: {ex.Message}");
                    logger.LogError(ex, "同步模板元数据失败: {TemplateId}", item.Id);
                }
            }

            if (stop || batch.Items.Count < _options.PageSize)
                break;

            page++;
        }

        await templates.SaveChangesAsync(ct);

        var state = await syncState.GetOrCreateAsync(ct);
        state.LastRunAt = DateTime.UtcNow;
        state.LastRunStatus = failed > 0 ? "partial" : "completed";
        state.LastRunProcessed = processed;
        state.LastRunErrors = errors.Take(20).ToList();
        await syncState.SaveAsync(state, ct);

        logger.LogInformation(
            "法天使模板元数据同步完成: processed={Processed}, skipped={Skipped}, failed={Failed}, contentProcessed={ContentProcessed}, contentFailed={ContentFailed}, watermark={Watermark}",
            processed, skipped, failed, contentProcessed, contentFailed, maxSourceUpdatedAt);

        return new TemplateSyncRunResult(processed, skipped, failed, errors);
    }

    private async Task UpsertTemplateVectorAsync(Domain.Templates.Template template, CancellationToken ct)
    {
        if (!await vectorStore.IsAvailableAsync(ct) || !embedding.IsConfigured)
            return;

        await vectorStore.EnsureCollectionsAsync(ct);
        var textBuilder = new StringBuilder();
        textBuilder.AppendLine($"标题：{template.Title}");
        if (!string.IsNullOrEmpty(template.Alias))
            textBuilder.AppendLine($"别名：{template.Alias}");
        if (!string.IsNullOrEmpty(template.Summary))
            textBuilder.AppendLine($"简介：{template.Summary}");
        if (!string.IsNullOrEmpty(template.Scenarios))
            textBuilder.AppendLine($"适用：{template.Scenarios}");
        if (template.Categories != null && template.Categories.Any())
            textBuilder.AppendLine($"合同分类：{string.Join(';', template.Categories)}");
        if (template.Tags != null && template.Tags.Any())
            textBuilder.AppendLine($"标签：{string.Join(' ', template.Tags)}");

        var vector = await embedding.EmbedAsync(textBuilder.ToString(), ct);
        if (vector is null) return;

        await vectorStore.UpsertTemplateAsync(template.Id, vector, new Dictionary<string, object>
        {
            ["number"] = template.Number,
            ["title"] = template.Title ?? string.Empty,
            ["alias"] = template.Alias ?? string.Empty,
            ["type"] = template.Type ?? string.Empty,
            ["categories"] = template.Categories ?? [],
            ["tags"] = template.Tags ?? [],
            ["summary"] = template.Summary ?? string.Empty,
            ["isOfficial"] = template.IsOfficial,
            ["ownerId"] = template.OwnerId.HasValue ? template.OwnerId.Value.ToString() : string.Empty,

        }, ct);
    }
}
