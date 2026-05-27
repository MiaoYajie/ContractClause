namespace ContractClause.Application.Common.Interfaces;

public record VectorSearchHit(Guid Id, double Score);

public interface IVectorStore
{
    Task<bool> IsAvailableAsync(CancellationToken ct = default);
    Task EnsureCollectionsAsync(CancellationToken ct = default);
    Task UpsertTemplateAsync(Guid id, float[] vector, IDictionary<string, object> payload, CancellationToken ct = default);
    Task UpsertClauseAsync(Guid id, float[] vector, IDictionary<string, object> payload, CancellationToken ct = default);
    Task<IReadOnlyList<VectorSearchHit>> SearchTemplatesAsync(float[] queryVector, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<VectorSearchHit>> SearchClausesAsync(float[] queryVector, Guid? templateId, string? clauseType, int limit, CancellationToken ct = default);
    Task DeleteTemplateAsync(Guid id, CancellationToken ct = default);
    Task DeleteClauseAsync(Guid id, CancellationToken ct = default);
}
