namespace ContractClause.Application.Common.Interfaces;

public interface ITemplateProcessedBlobWriter
{
    bool IsConfigured { get; }
    Task WriteProcessedFilesAsync(
        Guid templateId,
        IReadOnlyDictionary<string, string> files,
        CancellationToken ct = default);
}
