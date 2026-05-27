namespace ContractClause.Application.Common.Interfaces;

public interface IEmbeddingService
{
    Task<float[]?> EmbedAsync(string text, CancellationToken ct = default);
    bool IsConfigured { get; }
}
