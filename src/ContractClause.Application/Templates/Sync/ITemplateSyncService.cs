namespace ContractClause.Application.Templates.Sync;

public interface ITemplateSyncService
{
    Task<TemplateSyncRunResult> RunAsync(CancellationToken ct = default);
}

public record TemplateSyncRunResult(int Processed, int Skipped, int Failed, IReadOnlyList<string> Errors);
