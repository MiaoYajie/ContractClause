namespace ContractClause.Application.Common.Interfaces;

public interface ITemplateContentProcessingService
{
    Task<TemplateContentProcessingResult> ProcessAsync(Guid templateId, int version, CancellationToken ct = default);
}

public record TemplateContentProcessingResult(
    bool Success,
    int ClausesImported,
    string? Error = null);
