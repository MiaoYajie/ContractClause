using ContractClause.Application.Common.Interfaces;
using ContractClause.Domain.Templates;

namespace ContractClause.Application.Templates.Sync;

internal static class TemplateMetadataMapper
{
    public static Template ToNewEntity(FatianshiTemplateMetadata source, int number, DateTime now) => new()
    {
        Id = source.Id,
        Number = source.Number ?? number,
        Title = source.Title,
        Alias = source.Alias,
        Type = source.Type,
        Categories = source.Categories.ToList(),
        Tags = source.Tags.ToList(),
        Summary = source.Summary,
        Scenarios = source.Scenarios,
        SourceUpdatedAt = source.PublishedOn,
        IsOfficial = true,
        OwnerId = null,
        Version = source.Version,
        CreatedAt = now,
        UpdatedAt = now,
        IsDeleted = false
    };

    public static void ApplyUpdate(Template target, FatianshiTemplateMetadata source, DateTime now)
    {
        if (source.Number.HasValue)
            target.Number = source.Number.Value;
        target.Title = source.Title;
        target.Alias = source.Alias;
        target.Type = source.Type;
        target.Categories = source.Categories.ToList();
        target.Tags = source.Tags.ToList();
        target.Summary = source.Summary;
        target.Scenarios = source.Scenarios;
        target.SourceUpdatedAt = source.PublishedOn;
        target.IsOfficial = true;
        target.OwnerId = null;
        target.Version = source.Version;
        target.UpdatedAt = now;
        target.IsDeleted = false;
    }
}
