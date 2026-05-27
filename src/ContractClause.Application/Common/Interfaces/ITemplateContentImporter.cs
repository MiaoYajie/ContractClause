namespace ContractClause.Application.Common.Interfaces;

public interface ITemplateContentImporter
{
    Task<TemplateImportOutcome> ImportOrUpdateAsync(TemplateImportRequest request, CancellationToken ct = default);
}

public record TemplateImportOutcome(Guid TemplateId, int ClausesImported);

public record TemplateImportRequest(
    string HtmlContent,
    string Title,
    string Type,
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> Tags,
    bool IsOfficial,
    Guid? OwnerId,
    string? ExternalId = null,
    DateTime? SourceUpdatedAt = null,
    Guid? ExistingTemplateId = null);
