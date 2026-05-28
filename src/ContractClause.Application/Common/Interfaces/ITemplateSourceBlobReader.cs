namespace ContractClause.Application.Common.Interfaces;

public interface ITemplateSourceBlobReader
{
    bool IsConfigured { get; }
    Task<string?> ReadTemplateHtmlAsync(Guid templateId, int version, CancellationToken ct = default);
}
