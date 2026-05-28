using ContractClause.Application.Common.Interfaces;
using ContractClause.Domain.Clauses;
using ContractClause.Domain.Templates;

namespace ContractClause.Application.Templates;

public class TemplateContentImporter(
    ITemplateRepository templates,
    IClauseRepository clauses,
    IVectorStore vectorStore,
    IEmbeddingService embedding) : ITemplateContentImporter
{
    public async Task<TemplateImportOutcome> ImportOrUpdateAsync(TemplateImportRequest request, CancellationToken ct = default)
    {
        var markdown = TemplateContentProcessor.HtmlToMarkdown(request.HtmlContent);
        var outlineItems = TemplateContentProcessor.ExtractOutlineFromMarkdown(markdown);
        var now = DateTime.UtcNow;

        Template template;
        Outline outline;
        var isUpdate = request.ExistingTemplateId.HasValue;

        if (isUpdate)
        {
            template = await templates.GetByIdWithOutlineAsync(request.ExistingTemplateId!.Value, ct)
                ?? throw new InvalidOperationException($"模板 {request.ExistingTemplateId} 不存在");
            template.Title = request.Title;
            template.Type = request.Type;
            template.Categories = request.Categories.ToList();
            template.Tags = request.Tags.ToList();
            template.Summary = markdown.Length > 200 ? markdown[..200] : markdown;
            template.Scenarios = request.Title;
            template.IsOfficial = request.IsOfficial;
            template.UpdatedAt = now;
            template.Version++;

            outline = template.Outline ?? new Outline { Id = Guid.NewGuid(), TemplateId = template.Id, CreatedAt = now };
            outline.OutlineJson = TemplateContentProcessor.SerializeOutline(outlineItems);
            outline.UpdatedAt = now;
            template.Outline = outline;

            await clauses.SoftDeleteByTemplateAsync(template.Id, ct);
            await templates.UpdateAsync(template, ct);
        }
        else
        {
            var templateId = Guid.NewGuid();
            var number = await templates.GetNextNumberAsync(ct);
            template = new Template
            {
                Id = templateId,
                Number = number,
                Title = request.Title,
                Type = request.Type,
                Categories = request.Categories.ToList(),
                Tags = request.Tags.ToList(),
                Summary = markdown.Length > 200 ? markdown[..200] : markdown,
                Scenarios = request.Title,
                IsOfficial = request.IsOfficial,
                OwnerId = request.OwnerId,
                CreatedAt = now,
                UpdatedAt = now
            };

            outline = new Outline
            {
                Id = Guid.NewGuid(),
                TemplateId = templateId,
                OutlineJson = TemplateContentProcessor.SerializeOutline(outlineItems),
                CreatedAt = now,
                UpdatedAt = now
            };
            template.Outline = outline;
            await templates.AddAsync(template, ct);
        }

        var clauseEntities = TemplateContentProcessor.SegmentClauses(markdown, outlineItems, template.Id, now);
        await clauses.AddRangeAsync(clauseEntities, ct);
        await templates.SaveChangesAsync(ct);
        await clauses.SaveChangesAsync(ct);

        await UpsertVectorsAsync(template, clauseEntities, ct);
        return new TemplateImportOutcome(template.Id, clauseEntities.Count);
    }

    private async Task UpsertVectorsAsync(Template template, List<Clause> clauseEntities, CancellationToken ct)
    {
        if (!await vectorStore.IsAvailableAsync(ct) || !embedding.IsConfigured) return;

        await vectorStore.EnsureCollectionsAsync(ct);
        var tVec = await embedding.EmbedAsync($"{template.Title}\n{template.Summary}\n{template.Scenarios}", ct);
        if (tVec is not null)
        {
            await vectorStore.UpsertTemplateAsync(template.Id, tVec, new Dictionary<string, object>
            {
                ["title"] = template.Title,
                ["type"] = template.Type,
                ["categories"] = template.Categories,
                ["isOfficial"] = template.IsOfficial
            }, ct);
        }

        foreach (var clause in clauseEntities)
        {
            var cVec = await embedding.EmbedAsync(clause.Text.Length > 2000 ? clause.Text[..2000] : clause.Text, ct);
            if (cVec is null) continue;
            await vectorStore.UpsertClauseAsync(clause.Id, cVec, new Dictionary<string, object>
            {
                ["templateId"] = template.Id.ToString(),
                ["clauseType"] = clause.ClauseType,
                ["keywords"] = clause.Keywords
            }, ct);
            clause.VectorId = clause.Id.ToString();
        }

        await clauses.SaveChangesAsync(ct);
    }
}
